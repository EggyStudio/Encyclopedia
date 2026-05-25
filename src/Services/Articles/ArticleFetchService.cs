using System.Text;
using Encyclopedia.Models;
using Octokit;

namespace Encyclopedia.Services.Articles;

public sealed class ArticleFetchService : IArticleFetchService
{
    private readonly IGitHubClient _gh;
    private readonly IArticleParserService _parser;
    private readonly ILogger<ArticleFetchService> _log;

    public ArticleFetchService(IGitHubClient gh, IArticleParserService parser, ILogger<ArticleFetchService> log)
    {
        _gh     = gh;
        _parser = parser;
        _log    = log;
    }

    public async Task<IReadOnlyList<Article>> FetchAllAsync(WikiSource source, CancellationToken ct = default)
    {
        var dir = string.IsNullOrWhiteSpace(source.Meta.ArticlesDir) ? "articles" : source.Meta.ArticlesDir;

        IReadOnlyList<RepositoryContent> entries;
        try
        {
            entries = await _gh.Repository.Content.GetAllContentsByRef(
                source.Owner, source.Repo, dir, source.DefaultBranch);
        }
        catch (NotFoundException)
        {
            // No articles/ directory yet - that's fine, it means the workspace
            // has been created but nothing has been committed there.
            return [];
        }

        var articles = new List<Article>();
        foreach (var entry in entries)
        {
            if (entry.Type != ContentType.File) continue;
            if (!entry.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;

            var raw = await FetchTextAsync(source, entry, ct);
            if (raw is null) continue;

            var (front, body) = _parser.Parse(raw);
            if (string.IsNullOrWhiteSpace(front.Identifier))
            {
                _log.LogWarning("Article {Path} has no identifier; skipping", entry.Path);
                continue;
            }

            articles.Add(new Article
            {
                SourceId     = source.Id,
                Frontmatter  = front,
                RawMarkdown  = body,
                RelativePath = entry.Path,
                CommitSha    = entry.Sha,
            });
        }
        return articles;
    }

    public async Task<Article?> FetchOneAsync(WikiSource source, string relativePath, CancellationToken ct = default)
    {
        IReadOnlyList<RepositoryContent> entries;
        try
        {
            entries = await _gh.Repository.Content.GetAllContentsByRef(
                source.Owner, source.Repo, relativePath, source.DefaultBranch);
        }
        catch (NotFoundException) { return null; }

        var entry = entries.FirstOrDefault();
        if (entry is null) return null;

        var raw = await FetchTextAsync(source, entry, ct);
        if (raw is null) return null;
        var (front, body) = _parser.Parse(raw);
        if (string.IsNullOrWhiteSpace(front.Identifier)) return null;

        return new Article
        {
            SourceId     = source.Id,
            Frontmatter  = front,
            RawMarkdown  = body,
            RelativePath = entry.Path,
            CommitSha    = entry.Sha,
        };
    }

    // The contents API only returns inlined Content for files under ~1 MiB; for
    // anything larger we'd need the blob endpoint. Practical articles are
    // well under that, so the simple path is fine for now.
    private async Task<string?> FetchTextAsync(WikiSource source, RepositoryContent entry, CancellationToken ct)
    {
        if (entry.EncodedContent is not null && entry.Encoding == "base64")
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(entry.EncodedContent)); }
            catch { /* fall through */ }
        }

        if (!string.IsNullOrEmpty(entry.Content))
            return entry.Content;

        // Fallback: re-fetch the single file by path (the listing endpoint sometimes
        // returns entries without inline content for large dirs).
        try
        {
            var single = await _gh.Repository.Content.GetAllContentsByRef(
                source.Owner, source.Repo, entry.Path, source.DefaultBranch);
            var c = single.FirstOrDefault();
            if (c?.EncodedContent is not null)
                return Encoding.UTF8.GetString(Convert.FromBase64String(c.EncodedContent));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not fetch contents of {Path}", entry.Path);
        }
        return null;
    }
}
