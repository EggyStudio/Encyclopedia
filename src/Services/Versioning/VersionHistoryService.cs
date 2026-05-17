using Encyclopedia.Models;
using Octokit;

namespace Encyclopedia.Services.Versioning;

public sealed class VersionHistoryService : IVersionHistoryService
{
    private readonly IGitHubClient _gh;

    public VersionHistoryService(IGitHubClient gh) => _gh = gh;

    public Task<IReadOnlyList<VersionEntry>> GetHistoryAsync(WikiSource source, string articlePath, CancellationToken ct = default)
    {
        // TODO: gh.Repository.Commit.GetAll(owner, repo, new CommitRequest { Path = articlePath }).
        throw new NotImplementedException();
    }

    public Task<string?> GetRawAtAsync(WikiSource source, string articlePath, string commitSha, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<DiffResult> DiffAsync(WikiSource source, string articlePath, string fromSha, string toSha, CancellationToken ct = default)
        => throw new NotImplementedException();
}
