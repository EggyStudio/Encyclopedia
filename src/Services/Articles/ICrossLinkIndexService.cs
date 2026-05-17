using Encyclopedia.Models;

namespace Encyclopedia.Services.Articles;

public interface ICrossLinkIndexService
{
    /// <summary>Rebuild the global identifier -> URL index from all articles.</summary>
    Task RebuildAsync(CancellationToken ct = default);

    /// <summary>identifier (and aliases) -> canonical URL.</summary>
    Task<IReadOnlyDictionary<string, string>> GetIdentifierIndexAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Backlink>> GetBacklinksAsync(string identifier, CancellationToken ct = default);
}
