namespace Encyclopedia.Models;

public sealed record Asset
{
    public required string SourceId     { get; init; }
    public required string RelativePath { get; init; }   // assets/images/foo.png
    public required string ResolvedUrl  { get; init; }   // raw.githubusercontent.com/... or r2 public url
    public required AssetKind Kind      { get; init; }
    public long?   SizeBytes  { get; init; }
}

public enum AssetKind { Image, Video, File }
