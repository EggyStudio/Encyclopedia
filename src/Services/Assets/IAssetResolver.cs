using Encyclopedia.Models;

namespace Encyclopedia.Services.Assets;

public interface IAssetResolver
{
    /// <summary>
    /// Given a relative path inside an article (e.g. <c>assets/images/foo.png</c>),
    /// return the public URL it should resolve to. Backends are chosen per source.
    /// </summary>
    string Resolve(WikiSource source, string relativePath);

    /// <summary>Build the full asset index for one source (used at parse time).</summary>
    Task<IReadOnlyDictionary<string, string>> BuildIndexAsync(WikiSource source, CancellationToken ct = default);
}
