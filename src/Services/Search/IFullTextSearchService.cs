using Encyclopedia.Models;

namespace Encyclopedia.Services.Search;

public interface IFullTextSearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, SearchFilters filters, int limit = 25, CancellationToken ct = default);
    Task ReindexAsync(Article article, CancellationToken ct = default);
}

public sealed record SearchHit(
    string Identifier,
    string Title,
    string Snippet,
    double Rank,
    string SourceId);

public sealed record SearchFilters
{
    public string[] Tags        { get; init; } = [];
    public string[] Categories  { get; init; } = [];
    public string[] Sources     { get; init; } = [];
    public string?  Contributor { get; init; }
}
