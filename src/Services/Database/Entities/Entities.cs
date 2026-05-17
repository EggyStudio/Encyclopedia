using Encyclopedia.Models;

namespace Encyclopedia.Services.Database.Entities;

public sealed class WikiSourceEntity
{
    public required string          Id            { get; set; }   // owner/repo
    public required string          Owner         { get; set; }
    public required string          Repo          { get; set; }
    public required string          DefaultBranch { get; set; }
    public required WikiSourceTrust Trust         { get; set; }
    public required string          MetaJson      { get; set; }   // serialized WikiMeta
    public DateTime                 LastSyncedAt  { get; set; }
    public string?                  LastSyncedSha { get; set; }
}

public sealed class ArticleEntity
{
    public required string   Identifier      { get; set; }
    public required string   SourceId        { get; set; }
    public required string   Title           { get; set; }
    public required string   RelativePath    { get; set; }
    public required string   FrontmatterJson { get; set; }
    public required string   BodyMarkdown    { get; set; }
    public required string   CommitSha       { get; set; }
    public DateTime          FetchedAt       { get; set; }

    // Populated by trigger; only read.
    public string? SearchSnippet { get; set; }
}

public sealed class ArticleVersionEntity
{
    public required string   Identifier   { get; set; }
    public required string   CommitSha    { get; set; }
    public required string   AuthorLogin  { get; set; }
    public required string   AuthorName   { get; set; }
    public required DateTime CommittedAt  { get; set; }
    public required string   Message      { get; set; }
    public int               AdditionLines { get; set; }
    public int               DeletionLines { get; set; }
}

public sealed class IdentifierEntity
{
    public required string Slug              { get; set; }   // identifier OR alias
    public required string ArticleIdentifier { get; set; }   // canonical identifier
}

public sealed class CrossLinkEntity
{
    public required string SourceIdentifier { get; set; }
    public required string TargetIdentifier { get; set; }
    public int             Occurrences      { get; set; }
}

public sealed class TagEntity
{
    public required string Identifier { get; set; }
    public required string Tag        { get; set; }
}

public sealed class CategoryEntity
{
    public required string Identifier { get; set; }
    public required string Category   { get; set; }
}

public sealed class ContributorEntity
{
    public required string Identifier  { get; set; }
    public required string GithubLogin { get; set; }
    public int             Commits     { get; set; }
}

public sealed class AssetEntity
{
    public required string    SourceId     { get; set; }
    public required string    RelativePath { get; set; }
    public required string    ResolvedUrl  { get; set; }
    public required AssetKind Kind         { get; set; }
    public long?              SizeBytes    { get; set; }
}
