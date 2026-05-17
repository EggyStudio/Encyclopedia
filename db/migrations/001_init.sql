-- 001_init.sql - initial Encyclopedia schema for Postgres 15+.
-- Run via: psql -f db/migrations/001_init.sql
-- EF Core "ensure created" works for the relational shape, but the FTS column +
-- triggers below are owned by raw SQL because EF doesn't model tsvector cleanly.

BEGIN;

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;

-- ---------------------------------------------------------------------------
-- Sources
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS wiki_sources (
    id              VARCHAR(200) PRIMARY KEY,            -- owner/repo
    owner           TEXT          NOT NULL,
    repo            TEXT          NOT NULL,
    default_branch  TEXT          NOT NULL,
    trust           VARCHAR(20)   NOT NULL CHECK (trust IN ('Verified','OptIn','Discovered')),
    meta_json       JSONB         NOT NULL,
    last_synced_at  TIMESTAMPTZ   NOT NULL DEFAULT now(),
    last_synced_sha TEXT
);
CREATE INDEX IF NOT EXISTS idx_wiki_sources_trust ON wiki_sources (trust);

-- ---------------------------------------------------------------------------
-- Articles
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS articles (
    identifier        VARCHAR(200) PRIMARY KEY,
    source_id         VARCHAR(200) NOT NULL REFERENCES wiki_sources(id) ON DELETE CASCADE,
    title             TEXT         NOT NULL,
    relative_path     TEXT         NOT NULL,
    frontmatter_json  JSONB        NOT NULL,
    body_markdown     TEXT         NOT NULL,
    commit_sha        TEXT         NOT NULL,
    fetched_at        TIMESTAMPTZ  NOT NULL DEFAULT now(),
    search_vector     TSVECTOR
);
CREATE INDEX IF NOT EXISTS idx_articles_source ON articles (source_id);
CREATE INDEX IF NOT EXISTS idx_articles_fts    ON articles USING GIN (search_vector);
CREATE INDEX IF NOT EXISTS idx_articles_trgm   ON articles USING GIN (title gin_trgm_ops);

CREATE OR REPLACE FUNCTION articles_search_vector_trigger() RETURNS trigger AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('simple', unaccent(coalesce(NEW.title, ''))),         'A') ||
        setweight(to_tsvector('simple', unaccent(coalesce(NEW.body_markdown, ''))), 'B');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS articles_search_vector ON articles;
CREATE TRIGGER articles_search_vector
    BEFORE INSERT OR UPDATE OF title, body_markdown
    ON articles FOR EACH ROW
    EXECUTE FUNCTION articles_search_vector_trigger();

-- ---------------------------------------------------------------------------
-- Versions (one row per commit that touched an article file)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS article_versions (
    identifier      VARCHAR(200) NOT NULL,
    commit_sha      TEXT         NOT NULL,
    author_login    TEXT         NOT NULL,
    author_name     TEXT         NOT NULL,
    committed_at    TIMESTAMPTZ  NOT NULL,
    message         TEXT         NOT NULL,
    addition_lines  INT          NOT NULL DEFAULT 0,
    deletion_lines  INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (identifier, commit_sha)
);
CREATE INDEX IF NOT EXISTS idx_versions_identifier ON article_versions (identifier);
CREATE INDEX IF NOT EXISTS idx_versions_committed  ON article_versions (committed_at DESC);

-- ---------------------------------------------------------------------------
-- Identifiers (canonical + aliases) and crosslinks
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS identifiers (
    slug                 VARCHAR(200) PRIMARY KEY,
    article_identifier   VARCHAR(200) NOT NULL REFERENCES articles(identifier) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_identifiers_article ON identifiers (article_identifier);

CREATE TABLE IF NOT EXISTS crosslinks (
    source_identifier VARCHAR(200) NOT NULL REFERENCES articles(identifier) ON DELETE CASCADE,
    target_identifier VARCHAR(200) NOT NULL REFERENCES articles(identifier) ON DELETE CASCADE,
    occurrences       INT          NOT NULL DEFAULT 1,
    PRIMARY KEY (source_identifier, target_identifier)
);
CREATE INDEX IF NOT EXISTS idx_crosslinks_target ON crosslinks (target_identifier);

-- ---------------------------------------------------------------------------
-- Taxonomy
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS article_tags (
    identifier VARCHAR(200) NOT NULL REFERENCES articles(identifier) ON DELETE CASCADE,
    tag        TEXT         NOT NULL,
    PRIMARY KEY (identifier, tag)
);
CREATE INDEX IF NOT EXISTS idx_tags_tag ON article_tags (tag);

CREATE TABLE IF NOT EXISTS article_categories (
    identifier VARCHAR(200) NOT NULL REFERENCES articles(identifier) ON DELETE CASCADE,
    category   TEXT         NOT NULL,
    PRIMARY KEY (identifier, category)
);
CREATE INDEX IF NOT EXISTS idx_categories_category ON article_categories (category);

-- ---------------------------------------------------------------------------
-- Contributors
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS contributors (
    identifier   VARCHAR(200) NOT NULL REFERENCES articles(identifier) ON DELETE CASCADE,
    github_login TEXT         NOT NULL,
    commits      INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (identifier, github_login)
);
CREATE INDEX IF NOT EXISTS idx_contributors_login ON contributors (github_login);

-- ---------------------------------------------------------------------------
-- Assets (cached resolutions; the source of truth is GitHub or R2)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS assets (
    source_id     VARCHAR(200) NOT NULL REFERENCES wiki_sources(id) ON DELETE CASCADE,
    relative_path TEXT         NOT NULL,
    resolved_url  TEXT         NOT NULL,
    kind          VARCHAR(10)  NOT NULL CHECK (kind IN ('Image','Video','File')),
    size_bytes    BIGINT,
    PRIMARY KEY (source_id, relative_path)
);

COMMIT;
