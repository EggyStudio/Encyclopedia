using Encyclopedia.Models;

namespace Encyclopedia.Services.Assets;

public sealed class GitHubAssetProvider : IGitHubAssetProvider
{
    public string PublicUrl(WikiSource source, string relativePath)
        => $"https://raw.githubusercontent.com/{source.Owner}/{source.Repo}/{source.DefaultBranch}/{relativePath}";

    public Task<IReadOnlyList<string>> ListAsync(WikiSource source, CancellationToken ct = default)
    {
        // TODO: tree API call for assets dir.
        throw new NotImplementedException();
    }

    public Task UploadAsync(WikiSource source, string relativePath, Stream content, string githubToken, CancellationToken ct = default)
    {
        // TODO: client-side path: this is invoked from a server endpoint that proxies the user's
        // GitHub token (token never persisted server-side) to PUT /repos/{o}/{r}/contents/{path}.
        throw new NotImplementedException();
    }
}
