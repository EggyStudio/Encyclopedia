namespace Encyclopedia.Models;

public sealed record Article
{
    public required string      SourceId     { get; init; }   // FK -> WikiSource.Id
    public required Frontmatter Frontmatter  { get; init; }
    public required string      RawMarkdown  { get; init; }
    public required string      RelativePath { get; init; }   // articles/foo/bar.md
    public required string      CommitSha    { get; init; }   // GitHub sha at fetch time
    public DateTime             FetchedAt    { get; init; } = DateTime.UtcNow;
}
