namespace Encyclopedia.Models;

/// <summary>
/// A GitHub repository that has been registered as a source of articles for
/// the encyclopedia. Either auto-discovered (verified user) or opted-in via
/// the Discover page.
/// </summary>
public sealed record WikiSource
{
    public required string Id              { get; init; }   // <owner>/<repo>
    public required string Owner           { get; init; }
    public required string Repo            { get; init; }
    public required string DefaultBranch   { get; init; }
    public required WikiMeta Meta          { get; init; }
    public required WikiSourceTrust Trust  { get; init; }
    public DateTime LastSyncedAt           { get; init; }
    public string?  LastSyncedSha          { get; init; }
}

public enum WikiSourceTrust
{
    /// <summary>Listed in config/verified-users.yml; auto-included.</summary>
    Verified,
    /// <summary>Found by tag search; included after a viewer/operator opted in via Discover.</summary>
    OptIn,
    /// <summary>Discovered but not yet included.</summary>
    Discovered,
}
