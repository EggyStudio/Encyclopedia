# Encyclopedia - architecture

This document describes the runtime architecture and dataflow of the
encyclopedia. It is the spec the stubs in `src/Services/**` are written
against; once a stub's TODO is filled in, the behaviour described here is
the contract.

## 1. High-level dataflow

```
                                                       ┌──────────────┐
   GitHub repo ────.wiki-meta.yml───►  Discovery  ────►│ Postgres     │
   (articles/ +                       (GH search       │  wiki_sources│
    assets/)                           by topic +       └──────┬───────┘
        │                              meta fetch)            │
        │                                                     ▼
        │  raw .md  ┌───────────────┐ parsed Article  ┌──────────────┐
        └──────────►│ ArticleFetch  │────────────────►│ articles     │
                    │ + Parser      │                  │ identifiers  │
                    └──────┬────────┘                  │ crosslinks   │
                           │                           │ tags / cats  │
                           ▼                           │ contributors │
                    GitHub commit history              │ search_idx   │
                           │                           └──────┬───────┘
                           ▼                                  │
                    ┌─────────────────┐                       │
                    │ VersionHistory  │ ─────────────────────►│
                    └─────────────────┘                       │
                                                              ▼
   Browser ◄────────── Blazor server  ◄──── ArticleView ◄──── DB
```

## 2. Reader vs Editor surfaces

The same Blazor app serves both surfaces; route prefix selects the layout:

| Route prefix | Layout         | Visible to | Notes |
|--------------|----------------|------------|-------|
| `/`, `/wiki/*`, `/search`, `/discover`, `/tags`, `/categories`, `/contributors`, `/stats` | `ReaderLayout` | anyone | Wikipedia-style centered article, ToC, infobox, refs, backlinks. |
| `/edit/*`, `/profile` | `EditorLayout` | logged-in editors | Will switch to full BlueprintShell dock chrome once upstream change request #2 (ChromeMode) lands. |
| any, when UA is mobile | `MobileLayout` (auto) | anyone | Tabbed shell, PWA-friendly. |

`IDeviceDetectionService` reads the `User-Agent` server-side. The detection
is intentionally cheap; we do **not** server-side-render different markup
based on whether the client is mobile - instead the same DOM ships and CSS
media queries collapse the wiki article grid below 768px.

## 3. Authentication model - "client-side accounts"

There is no server-side user table. The entire account lives in a JSON
file the user keeps:

1. User clicks **Create account** in `/login`. The browser generates a UUID
   and a JSON file with their display name and (optionally) email.
2. User downloads the file, keeps it. (Browser also stashes a copy in
   `localStorage` for the current device.)
3. On return visits / new devices, user uploads the file via
   `AccountFileUpload.razor`. Schema is validated by
   `ClientAccountService.Validate`.
4. When the user adds a **GitHub Personal Access Token**, the token is
   stored only inside this file (and in `localStorage` for convenience).
   The server sees the token only on the request that performs the
   commit, and never persists it.
5. Same model for the optional **Cloudflare R2 access keys**.

Trust note: this model means the server cannot verify a user's identity in
the way a session cookie would. We rely on GitHub itself as the trust
anchor - every contribution is a commit signed by the user's GitHub
account (via their token), so authorship is verifiable downstream the
same way it is on a normal git repo.

## 4. Article fetch + parse pipeline

`IArticleFetchService.FetchAllAsync(source)` performs:

1. Get default-branch HEAD sha for `source`.
2. List `source.Meta.ArticlesDir` recursively via Git tree API.
3. For each `*.md` file:
    - Pull raw content via `git/blobs/{sha}` (avoids per-file API round trips).
    - `Parser.Parse(raw)` splits on the leading `---` block and YAML-deserializes
      into `Frontmatter`. Missing identifier is synthesized from the slugified
      relative path; missing title is the first H1 of the body.
4. Persist each resulting `Article` into the `articles` table (the FTS trigger
   maintains `search_vector` automatically).

## 5. Asset resolution

`IAssetResolver.Resolve(source, relativePath)` picks one of two backends
based on `source.Meta.Assets`:

- `Github` (default): `https://raw.githubusercontent.com/{owner}/{repo}/{branch}/{path}`
- `R2`:               `{publicBase}/{path}` using the R2 config from the
  source's `.wiki-meta.yml`.

A per-source `AssetIndex` is built once at parse time and threaded into
`ArticleRenderContext.AssetIndex`. The parser's Markdig extension rewrites
local `image` / `video` / `file` references through this map.

## 6. Auto-crosslinking

Identifiers and their aliases are collected into a single global map
(`identifiers` table). At render time:

1. Iterate the Markdig AST.
2. For every plain text node in a paragraph, scan for whole-word matches
   against the identifier set, case-insensitive, longest-match-first.
3. Wrap the first occurrence per paragraph in `<a class="crosslink"
   href="/wiki/{target}">…</a>`. Subsequent occurrences left as plain text
   to avoid link spam.
4. Record the resulting `(source, target)` pairs into `crosslinks`. The
   reverse direction populates the **What links here** section.

## 7. Search

PostgreSQL FTS via the `search_vector` column on `articles`, plus a `pg_trgm`
GIN index on `title` for typo-tolerant title autocomplete. `SearchFilters`
adds optional WHERE clauses against `article_tags`, `article_categories`,
and `wiki_sources.id`.

## 8. Version history

`IVersionHistoryService` defers to the GitHub commits API filtered by
file path. Diffs use the `compare/{base}...{head}` API; result is a
`DiffResult` carrying the unified diff text. We cache the latest N
revisions per article in `article_versions` for snappy timeline pages.

## 9. Discover / opt-in

`Discover.razor` lists:

- All `WikiSourceTrust.Verified` sources (auto-included).
- `OptIn` sources (already approved).
- `Discovered` sources with an **Include** button. Admins (initially: any
  user listed under `config/verified-users.yml` `owners:`) can promote
  Discovered → OptIn.

## 10. Deploy lifecycle

```
git push main
   │
   ▼
GitHub Actions  (build-and-deploy.yml)
   ├─ docker build → ghcr.io/<owner>/<repo>:sha-<sha>
   ├─ tag :latest on main
   └─ flyctl deploy --image ... --strategy rolling
                                          │
                                          ▼
                                Fly.io pulls the new image,
                                spins up a new machine, drains
                                the old one. Zero downtime.
```

## 11. Open work

Anything labeled `// TODO:` in `src/**` plus the items in
`docs/BlueprintShell-changes.md` (P0 items in particular).
