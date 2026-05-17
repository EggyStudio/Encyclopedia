namespace Encyclopedia.Models;

/// <summary>
/// Auto-built crosslink between two articles. The reader rewriter scans for
/// occurrences of <see cref="TargetIdentifier"/> (and any aliases) in source
/// article text and turns them into links. No manual [[wiki-link]] needed.
/// </summary>
public sealed record CrossLink
{
    public required string SourceIdentifier { get; init; }
    public required string TargetIdentifier { get; init; }
    public required int    Occurrences      { get; init; }
}

/// <summary>Reverse index of <see cref="CrossLink"/>.</summary>
public sealed record Backlink
{
    public required string TargetIdentifier { get; init; }
    public required string SourceIdentifier { get; init; }
    public required string SourceTitle      { get; init; }
}
