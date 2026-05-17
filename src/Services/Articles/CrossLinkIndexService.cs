using Encyclopedia.Models;
using Encyclopedia.Services.Database;

namespace Encyclopedia.Services.Articles;

public sealed class CrossLinkIndexService : ICrossLinkIndexService
{
    private readonly EncyclopediaDbContext _db;

    public CrossLinkIndexService(EncyclopediaDbContext db) => _db = db;

    public Task RebuildAsync(CancellationToken ct = default)
    {
        // TODO: enumerate all articles, for each frontmatter populate identifiers
        // table (identifier + aliases -> article_id). Then second pass to scan
        // each article body for occurrences of other identifiers, write crosslinks
        // and backlinks tables. Wrap in a single transaction.
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<string, string>> GetIdentifierIndexAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<Backlink>> GetBacklinksAsync(string identifier, CancellationToken ct = default)
        => throw new NotImplementedException();
}
