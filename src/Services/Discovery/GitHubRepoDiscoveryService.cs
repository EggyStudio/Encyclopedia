using Encyclopedia.Models;
using Octokit;

namespace Encyclopedia.Services.Discovery;

public sealed class GitHubRepoDiscoveryService : IGitHubRepoDiscoveryService
{
    private const string DiscoveryTopic = "encyclopedia-wiki";
    private readonly IGitHubClient _gh;
    private readonly ILogger<GitHubRepoDiscoveryService> _log;

    public GitHubRepoDiscoveryService(IGitHubClient gh, ILogger<GitHubRepoDiscoveryService> log)
    {
        _gh  = gh;
        _log = log;
    }

    public Task<IReadOnlyList<WikiSource>> DiscoverAsync(CancellationToken ct = default)
    {
        // TODO: search repositories by topic:encyclopedia-wiki, page through results,
        // for each candidate fetch /.wiki-meta.yml on the default branch and parse it,
        // build a WikiSource with Trust = Discovered (registry promotes to Verified/OptIn).
        throw new NotImplementedException();
    }

    public Task<WikiSource?> RefreshAsync(string ownerSlashRepo, CancellationToken ct = default)
    {
        // TODO: fetch repo, fetch .wiki-meta.yml, parse, return updated WikiSource.
        throw new NotImplementedException();
    }
}
