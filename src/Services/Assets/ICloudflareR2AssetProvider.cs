using Encyclopedia.Models;

namespace Encyclopedia.Services.Assets;

public interface ICloudflareR2AssetProvider
{
    string PublicUrl(WikiSource source, string relativePath);
    Task<IReadOnlyList<string>> ListAsync(WikiSource source, CancellationToken ct = default);
    Task UploadAsync(WikiSource source, string relativePath, Stream content, string accessKey, string secretKey, CancellationToken ct = default);
}
