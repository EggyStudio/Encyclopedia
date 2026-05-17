using System.Text;
using Octokit;
using WikiSource = Encyclopedia.Models.WikiSource;

namespace Encyclopedia.Services.Auth;

public sealed class GitHubPushService : IGitHubPushService
{
    public async Task<CommitResult> CommitFileAsync(
        WikiSource source,
        string filePath,
        byte[]    content,
        string    commitMessage,
        string    authorName,
        string    authorEmail,
        string    githubToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(githubToken))
            throw new InvalidOperationException("No GitHub token; cannot commit.");

        var gh = new GitHubClient(new ProductHeaderValue("Encyclopedia"))
        {
            Credentials = new Credentials(githubToken),
        };

        var contentText = Encoding.UTF8.GetString(content);
        var committer   = new Committer(authorName, authorEmail, DateTimeOffset.UtcNow);

        // Probe for an existing file to decide create vs update.
        string? existingSha = null;
        try
        {
            var existing = await gh.Repository.Content.GetAllContentsByRef(
                source.Owner, source.Repo, filePath, source.DefaultBranch);
            if (existing.Count > 0) existingSha = existing[0].Sha;
        }
        catch (NotFoundException) { /* new file */ }

        if (existingSha is null)
        {
            var req = new CreateFileRequest(commitMessage, contentText, source.DefaultBranch)
            {
                Committer = committer,
                Author    = committer,
            };
            var result = await gh.Repository.Content.CreateFile(source.Owner, source.Repo, filePath, req);
            return new CommitResult(result.Commit.Sha, CommitUrl(source, result.Commit.Sha));
        }
        else
        {
            var req = new UpdateFileRequest(commitMessage, contentText, existingSha, source.DefaultBranch)
            {
                Committer = committer,
                Author    = committer,
            };
            var result = await gh.Repository.Content.UpdateFile(source.Owner, source.Repo, filePath, req);
            return new CommitResult(result.Commit.Sha, CommitUrl(source, result.Commit.Sha));
        }
    }

    private static string CommitUrl(WikiSource source, string sha)
        => $"https://github.com/{source.Owner}/{source.Repo}/commit/{sha}";
}
