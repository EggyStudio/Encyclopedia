using Encyclopedia.Models;

namespace Encyclopedia.Services.Discovery;

public interface IGitHubRepoDiscoveryService
{
    /// <summary>
    /// Scan GitHub for repos that advertise themselves as encyclopedia sources.
    /// Today this means: repos with the topic <c>encyclopedia-wiki</c> AND a
    /// <c>.wiki-meta.yml</c> at the root. Returns all matches; trust is decided
    /// by <see cref="IWikiSourceRegistry"/>.
    /// </summary>
    Task<IReadOnlyList<WikiSource>> DiscoverAsync(CancellationToken ct = default);

    /// <summary>Re-fetch a single source's <c>.wiki-meta.yml</c> + branch HEAD sha.</summary>
    Task<WikiSource?> RefreshAsync(string ownerSlashRepo, CancellationToken ct = default);
}
