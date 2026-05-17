using Encyclopedia.Models;
using Octokit;

namespace Encyclopedia.Services.Articles;

public sealed class ArticleFetchService : IArticleFetchService
{
    private readonly IGitHubClient _gh;
    private readonly IArticleParserService _parser;

    public ArticleFetchService(IGitHubClient gh, IArticleParserService parser)
    {
        _gh     = gh;
        _parser = parser;
    }

    public Task<IReadOnlyList<Article>> FetchAllAsync(WikiSource source, CancellationToken ct = default)
    {
        // TODO: walk source.Meta.ArticlesDir on the default branch, pull each .md,
        // parse frontmatter via _parser.Parse(...), return list of Article.
        throw new NotImplementedException();
    }

    public Task<Article?> FetchOneAsync(WikiSource source, string relativePath, CancellationToken ct = default)
        => throw new NotImplementedException();
}
