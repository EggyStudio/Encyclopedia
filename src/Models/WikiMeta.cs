namespace Encyclopedia.Models;

/// <summary>
/// Shape of <c>.wiki-meta.yml</c> at the root of a GitHub repo that wants
/// to be discoverable by the encyclopedia. Repos without this file are
/// ignored by the discovery service.
/// </summary>
public sealed record WikiMeta
{
    public required string Identifier  { get; init; }   // unique slug across all wikis
    public required string Title       { get; init; }
    public string?         Description { get; init; }
    public required string Owner       { get; init; }   // GitHub login
    public string?         License     { get; init; }
    public string?         Language    { get; init; }   // BCP-47, e.g. "en"
    public string[]        Tags        { get; init; } = [];
    public string[]        Categories  { get; init; } = [];
    public string?         Homepage    { get; init; }
    public AssetBackend    Assets      { get; init; } = AssetBackend.Github;
    public R2Config?       R2          { get; init; }
    public string          ArticlesDir { get; init; } = "articles";
    public string          AssetsDir   { get; init; } = "assets";
}

public enum AssetBackend { Github, R2 }

public sealed record R2Config
{
    public required string AccountId  { get; init; }
    public required string BucketName { get; init; }
    public required string PublicBase { get; init; }   // https://pub-xxx.r2.dev or custom domain
}
