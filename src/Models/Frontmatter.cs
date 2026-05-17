namespace Encyclopedia.Models;

/// <summary>
/// YAML frontmatter parsed off the top of each article markdown file.
/// Only <c>Identifier</c> + <c>Title</c> are required.
/// </summary>
public sealed record Frontmatter
{
    public required string Identifier { get; init; }
    public required string Title      { get; init; }

    public string?   Summary      { get; init; }
    public string[]  Tags         { get; init; } = [];
    public string[]  Categories   { get; init; } = [];
    public string[]  Aliases      { get; init; } = [];   // alternate identifiers that map to this article
    public string[]  Sources      { get; init; } = [];   // URLs shown in the "References" section
    public InfoBoxData? InfoBox   { get; init; }
    public string?   Image        { get; init; }         // hero image, relative path under assets/
    public DateTime? Published    { get; init; }
    public DateTime? Updated      { get; init; }
}

public sealed record InfoBoxData
{
    public string? Image { get; init; }
    public string? Caption { get; init; }
    public Dictionary<string, string> Fields { get; init; } = new();
}
