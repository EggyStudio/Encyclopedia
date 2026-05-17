using Encyclopedia.Models;
using Octokit;

namespace Encyclopedia.Services.Auth;

public sealed class GitHubPushService : IGitHubPushService
{
    public Task<CommitResult> CommitFileAsync(
        WikiSource source,
        string filePath,
        byte[]    content,
        string    commitMessage,
        string    authorName,
        string    authorEmail,
        string    githubToken,
        CancellationToken ct = default)
    {
        // TODO: build a per-request GitHubClient with new Credentials(githubToken).
        // For new files: gh.Repository.Content.CreateFile(...).
        // For updates:   GET existing file SHA, then UpdateFile(...).
        // Author/Committer = new Committer(authorName, authorEmail, DateTimeOffset.UtcNow).
        // Return commit sha + html_url.
        throw new NotImplementedException();
    }
}
