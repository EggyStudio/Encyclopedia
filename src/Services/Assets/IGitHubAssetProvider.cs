using Encyclopedia.Models;

namespace Encyclopedia.Services.Assets;

public interface IGitHubAssetProvider
{
    string PublicUrl(WikiSource source, string relativePath);
    Task<IReadOnlyList<string>> ListAsync(WikiSource source, CancellationToken ct = default);
    Task UploadAsync(WikiSource source, string relativePath, Stream content, string githubToken, CancellationToken ct = default);
}
