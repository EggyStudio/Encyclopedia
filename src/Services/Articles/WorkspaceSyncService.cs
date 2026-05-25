using System.Text.Json;
using Encyclopedia.Models;
using Encyclopedia.Services.Database;
using Encyclopedia.Services.Database.Entities;
using Encyclopedia.Services.Discovery;
using Microsoft.EntityFrameworkCore;

namespace Encyclopedia.Services.Articles;

public sealed class WorkspaceSyncService : IWorkspaceSyncService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly EncyclopediaDbContext _db;
    private readonly IWikiSourceRegistry   _registry;
    private readonly IArticleFetchService  _fetch;
    private readonly ILogger<WorkspaceSyncService> _log;

    public WorkspaceSyncService(
        EncyclopediaDbContext db,
        IWikiSourceRegistry registry,
        IArticleFetchService fetch,
        ILogger<WorkspaceSyncService> log)
    {
        _db       = db;
        _registry = registry;
        _fetch    = fetch;
        _log      = log;
    }

    public async Task<SyncResult> SyncWorkspaceAsync(
        Account account,
        WorkspaceRepo workspace,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report("Registering your workspace…");
        var source = BuildSource(account, workspace);
        await _registry.UpsertAsync(source, ct);

        progress?.Report("Fetching articles from GitHub…");
        var fetched = await _fetch.FetchAllAsync(source, ct);

        progress?.Report($"Indexing {fetched.Count} article(s)…");
        var upserted = 0;
        foreach (var article in fetched)
        {
            await UpsertArticleAsync(article, ct);
            upserted++;
        }

        // Anything in the DB for this source that's no longer on GitHub is stale.
        var fetchedIds = fetched.Select(a => a.Frontmatter.Identifier).ToHashSet();
        var stale = await _db.Articles
            .Where(a => a.SourceId == source.Id && !fetchedIds.Contains(a.Identifier))
            .ToListAsync(ct);

        if (stale.Count > 0)
        {
            _db.Articles.RemoveRange(stale);
            await _db.SaveChangesAsync(ct);
        }

        _log.LogInformation("Sync of {Source}: {Upserted} upserted, {Removed} removed",
            source.Id, upserted, stale.Count);
        return new SyncResult(upserted, stale.Count, fetched.Count);
    }

    private async Task UpsertArticleAsync(Article article, CancellationToken ct)
    {
        var id = article.Frontmatter.Identifier;
        var fmJson = JsonSerializer.Serialize(article.Frontmatter, JsonOpts);

        var existing = await _db.Articles.FirstOrDefaultAsync(a => a.Identifier == id, ct);
        if (existing is null)
        {
            _db.Articles.Add(new ArticleEntity
            {
                Identifier      = id,
                SourceId        = article.SourceId,
                Title           = article.Frontmatter.Title,
                RelativePath    = article.RelativePath,
                FrontmatterJson = fmJson,
                BodyMarkdown    = article.RawMarkdown,
                CommitSha       = article.CommitSha,
                FetchedAt       = DateTime.UtcNow,
            });
        }
        else
        {
            existing.SourceId        = article.SourceId;
            existing.Title           = article.Frontmatter.Title;
            existing.RelativePath    = article.RelativePath;
            existing.FrontmatterJson = fmJson;
            existing.BodyMarkdown    = article.RawMarkdown;
            existing.CommitSha       = article.CommitSha;
            existing.FetchedAt       = DateTime.UtcNow;
        }

        // Replace tag / category rows for this article. Cheaper than diffing
        // for small sets and avoids stale entries when frontmatter changes.
        var tagsExisting = await _db.Tags.Where(t => t.Identifier == id).ToListAsync(ct);
        _db.Tags.RemoveRange(tagsExisting);
        foreach (var t in article.Frontmatter.Tags.Where(s => !string.IsNullOrWhiteSpace(s)))
            _db.Tags.Add(new TagEntity { Identifier = id, Tag = t.Trim() });

        var catsExisting = await _db.Categories.Where(c => c.Identifier == id).ToListAsync(ct);
        _db.Categories.RemoveRange(catsExisting);
        foreach (var c in article.Frontmatter.Categories.Where(s => !string.IsNullOrWhiteSpace(s)))
            _db.Categories.Add(new CategoryEntity { Identifier = id, Category = c.Trim() });

        await _db.SaveChangesAsync(ct);
    }

    private static WikiSource BuildSource(Account account, WorkspaceRepo workspace) => new()
    {
        Id            = $"{workspace.Owner}/{workspace.Repo}",
        Owner         = workspace.Owner,
        Repo          = workspace.Repo,
        DefaultBranch = workspace.DefaultBranch,
        Trust         = WikiSourceTrust.OptIn,
        LastSyncedAt  = DateTime.UtcNow,
        Meta = new WikiMeta
        {
            Identifier  = workspace.Identifier,
            Title       = $"{account.DisplayName}'s Encyclopedia",
            Owner       = workspace.Owner,
            ArticlesDir = "articles",
            AssetsDir   = "assets",
        },
    };
}
