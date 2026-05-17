namespace Encyclopedia.Models;

/// <summary>
/// Client-side account, persisted as a JSON file the user downloads and re-uploads,
/// and cached in browser localStorage for session continuity. The server never
/// stores this file; it lives in browser local storage between sessions and is
/// re-bootstrapped from the uploaded file.
/// </summary>
public sealed record Account
{
    public required string Id           { get; init; }   // random uuid, also the public handle
    public required string DisplayName  { get; init; }
    public string?         Email        { get; init; }   // optional, for git commit author
    public string?         GithubLogin  { get; init; }
    public string?         GithubToken  { get; init; }   // PAT or fine-grained token; NEVER sent to server except for per-request GitHub calls
    public string?         R2AccessKey  { get; init; }
    public string?         R2SecretKey  { get; init; }
    public WorkspaceRepo?  Workspace    { get; init; }   // auto-created wiki repo, if any
    public DateTime        CreatedAt    { get; init; } = DateTime.UtcNow;
    public int             SchemaVersion { get; init; } = 1;
}

/// <summary>
/// Metadata about the GitHub repo that holds this user's articles. Created
/// automatically when the user connects a token; subsequent article edits
/// commit straight into it.
/// </summary>
public sealed record WorkspaceRepo
{
    public required string   Owner         { get; init; }   // github login that owns the repo
    public required string   Repo          { get; init; }   // repo name
    public required string   DefaultBranch { get; init; }
    public required string   Identifier    { get; init; }   // matches .wiki-meta.yml identifier
    public required DateTime CreatedAt     { get; init; }
    public string?           HtmlUrl       { get; init; }
}
