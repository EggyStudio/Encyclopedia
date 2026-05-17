namespace Encyclopedia.Models;

/// <summary>
/// One revision of an article, sourced from the GitHub commit history of its file.
/// </summary>
public sealed record VersionEntry
{
    public required string   Identifier   { get; init; }
    public required string   SourceId     { get; init; }
    public required string   CommitSha    { get; init; }
    public required string   AuthorLogin  { get; init; }
    public required string   AuthorName   { get; init; }
    public required DateTime CommittedAt  { get; init; }
    public required string   Message      { get; init; }
    public int               AdditionLines { get; init; }
    public int               DeletionLines { get; init; }
}
