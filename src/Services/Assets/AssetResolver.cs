using Encyclopedia.Models;

namespace Encyclopedia.Services.Assets;

public sealed class AssetResolver : IAssetResolver
{
    private readonly IGitHubAssetProvider _gh;
    private readonly ICloudflareR2AssetProvider _r2;

    public AssetResolver(IGitHubAssetProvider gh, ICloudflareR2AssetProvider r2)
    {
        _gh = gh;
        _r2 = r2;
    }

    public string Resolve(WikiSource source, string relativePath) => source.Meta.Assets switch
    {
        AssetBackend.Github => _gh.PublicUrl(source, relativePath),
        AssetBackend.R2     => _r2.PublicUrl(source, relativePath),
        _ => throw new NotSupportedException($"Unknown asset backend: {source.Meta.Assets}"),
    };

    public Task<IReadOnlyDictionary<string, string>> BuildIndexAsync(WikiSource source, CancellationToken ct = default)
    {
        // TODO: enumerate files under source.Meta.AssetsDir, return map of relative path -> resolved URL.
        throw new NotImplementedException();
    }
}
