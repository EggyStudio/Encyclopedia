using Encyclopedia.Models;

namespace Encyclopedia.Services.Assets;

public sealed class CloudflareR2AssetProvider : ICloudflareR2AssetProvider
{
    public string PublicUrl(WikiSource source, string relativePath)
    {
        var r2 = source.Meta.R2 ?? throw new InvalidOperationException(
            $"Source {source.Id} declares asset backend = R2 but no r2 config in .wiki-meta.yml");
        return $"{r2.PublicBase.TrimEnd('/')}/{relativePath}";
    }

    public Task<IReadOnlyList<string>> ListAsync(WikiSource source, CancellationToken ct = default)
    {
        // TODO: S3 ListObjectsV2 against the source's bucket via AWSSDK.S3 with the R2 endpoint.
        throw new NotImplementedException();
    }

    public Task UploadAsync(WikiSource source, string relativePath, Stream content, string accessKey, string secretKey, CancellationToken ct = default)
    {
        // TODO: S3 PutObject with the R2 endpoint url derived from r2.AccountId.
        throw new NotImplementedException();
    }
}
