using Encyclopedia.Models;

namespace Encyclopedia.Services.Auth;

public interface IGitHubPushService
{
    /// <summary>
    /// Commit a single file change (article or asset) to a source repo using the
    /// user's GitHub token. The token is not stored server-side; it's accepted
    /// per-request from the client and used only for this call.
    /// </summary>
    Task<CommitResult> CommitFileAsync(
        WikiSource source,
        string filePath,
        byte[]    content,
        string    commitMessage,
        string    authorName,
        string    authorEmail,
        string    githubToken,
        CancellationToken ct = default);
}

public sealed record CommitResult(string Sha, string CommitUrl);
