# Encyclopedia

An open, Wikipedia-style encyclopedia where articles live in ordinary GitHub
repositories and the web app pulls them in automatically. Built on Blazor +
[BlueprintShell] / BlazorBlueprint, indexed in PostgreSQL, deployed via
GitHub Actions → `ghcr.io` → Fly.io.

[BlueprintShell]: https://github.com/EggyStudio/BlueprintShell

## Quickstart

```bash
# 1. Bring up the database. The first time the container starts on an empty
#    volume, every *.sql file in db/migrations/ runs automatically (it's
#    mounted into /docker-entrypoint-initdb.d).
docker compose up -d postgres

# 2. (Optional) verify the schema applied:
docker compose exec postgres psql -U encyclopedia -d encyclopedia -c "\dt"

# 3. Run the app locally
dotnet run --project Encyclopedia.csproj
# -> http://localhost:5000
```

If you ever change a SQL migration file, the auto-init only runs on a fresh
volume. To re-apply against the existing volume, pipe it in manually
(give Postgres a couple of seconds to be ready first):

```bash
docker compose exec -T postgres psql -U encyclopedia -d encyclopedia < db/migrations/001_init.sql
```

Or destroy the volume and let auto-init run again: `docker compose down -v`.

To build & run everything in containers:

```bash
docker compose up --build
```

## Architecture (one paragraph)

Articles are stored as `*.md` files in **GitHub repos** that opt in by
publishing a root-level `.wiki-meta.yml` (see `docs/wiki-meta.example.yml`)
and applying the topic `encyclopedia-wiki`. A background discovery service
finds them, the registry promotes verified owners (listed in
`config/verified-users.yml`) to auto-include and parks the rest as
**Discovered** until an operator opts them in from the Discover page.
A parser pipeline extracts YAML frontmatter, rewrites asset paths (GitHub
raw URLs or Cloudflare R2), and renders the markdown body through Markdig
with an extension that auto-links any text matching another article's
identifier (no manual `[[wiki-links]]`). Postgres holds the search index
(`tsvector` + GIN), the crosslink/backlink tables, version history, and
the taxonomy (tags/categories/contributors). User accounts are
**client-side only** — a JSON file the user downloads and re-uploads; the
server never persists it. Edits are pushed back to GitHub using the user's
own token, which is never stored. The shell decides which surface to
render — Wikipedia-like reader for visitors, a BlueprintShell-driven
editor for authenticated contributors, and a PWA-friendly mobile stack
when the user-agent indicates a phone.

See `docs/BlueprintShell-changes.md` for changes requested upstream in the
BlueprintShell package itself, and `docs/architecture.md` for component
diagrams and dataflow detail.

## Deploy

`main` triggers `.github/workflows/build-and-deploy.yml`, which builds a
multi-stage Docker image, pushes `sha-<commit>` + `latest` to
`ghcr.io/<owner>/<repo>`, then `flyctl deploy --image ...` with a rolling
strategy for zero-downtime.

Required GitHub Actions secrets:

- `FLY_API_TOKEN` — `fly tokens create deploy` output.

Required Fly.io secrets:

- `ConnectionStrings__Postgres` — set automatically by `fly postgres attach`.
- `GITHUB_TOKEN` — optional, increases Octokit rate limits for discovery.

## Project layout

```
.
├── Program.cs                       # ASP.NET Core host wiring + DI
├── Encyclopedia.csproj
├── Dockerfile / docker-compose.yml
├── fly.toml
├── .github/workflows/               # CI/CD
├── config/verified-users.yml        # static allowlist
├── db/migrations/                   # raw SQL (Postgres FTS triggers)
├── docs/
│   ├── BlueprintShell-changes.md    # change request for upstream
│   ├── architecture.md
│   └── wiki-meta.example.yml
├── src/
│   ├── Components/                  # Blazor routes + shared components
│   │   ├── Layout/{Reader,Editor,Mobile}Layout.razor
│   │   ├── Pages/                   # routed pages
│   │   ├── Article/                 # ArticleView, InfoBox, ToC, …
│   │   ├── Editor/                  # MarkdownEditor, AssetUploader, …
│   │   ├── Search/
│   │   └── Auth/
│   ├── Models/                      # POCOs (WikiMeta, Article, Frontmatter, …)
│   └── Services/                    # interfaces + stubs grouped by domain
│       ├── Discovery/
│       ├── Articles/
│       ├── Assets/
│       ├── Search/
│       ├── Versioning/
│       ├── Auth/
│       ├── Database/
│       └── Mobile/
└── wwwroot/                         # PWA manifest + sw.js + css/js
```

## Status

This is the scaffolding pass. Every service has interface + DI wiring; the
method bodies marked `// TODO:` are the next iteration's work, plus the
items in `docs/BlueprintShell-changes.md` that depend on upstream support.
