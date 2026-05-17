using Encyclopedia.Models;

namespace Encyclopedia.Services.Versioning;

public interface IVersionHistoryService
{
    Task<IReadOnlyList<VersionEntry>> GetHistoryAsync(WikiSource source, string articlePath, CancellationToken ct = default);
    Task<string?> GetRawAtAsync(WikiSource source, string articlePath, string commitSha, CancellationToken ct = default);
    Task<DiffResult> DiffAsync(WikiSource source, string articlePath, string fromSha, string toSha, CancellationToken ct = default);
}

public sealed record DiffResult(string Unified, int AdditionLines, int DeletionLines);
