using Encyclopedia.Models;
using Encyclopedia.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace Encyclopedia.Services.Search;

public sealed class FullTextSearchService : IFullTextSearchService
{
    private readonly EncyclopediaDbContext _db;

    public FullTextSearchService(EncyclopediaDbContext db) => _db = db;

    public Task<IReadOnlyList<SearchHit>> SearchAsync(string query, SearchFilters filters, int limit = 25, CancellationToken ct = default)
    {
        // TODO: select identifier, title, ts_headline(...) as snippet, ts_rank_cd(search_vector, plainto_tsquery(...)) as rank
        // FROM search_index JOIN articles ... WHERE search_vector @@ plainto_tsquery(@q)
        // AND tag/category/source filters. ORDER BY rank DESC LIMIT @limit. Use raw SQL via _db.Database.SqlQueryRaw.
        throw new NotImplementedException();
    }

    public Task ReindexAsync(Article article, CancellationToken ct = default)
    {
        // TODO: upsert into search_index with to_tsvector('simple', title || ' ' || body).
        throw new NotImplementedException();
    }
}
