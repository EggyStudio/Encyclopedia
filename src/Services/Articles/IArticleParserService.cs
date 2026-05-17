using Encyclopedia.Models;

namespace Encyclopedia.Services.Articles;

public interface IArticleParserService
{
    /// <summary>Split frontmatter from body and parse both.</summary>
    (Frontmatter Front, string Body) Parse(string rawMarkdown);

    /// <summary>
    /// Render markdown body to safe HTML with assets resolved, crosslinks
    /// inserted, and source-links footnoted Wikipedia-style.
    /// </summary>
    string RenderHtml(string body, ArticleRenderContext ctx);
}

public sealed record ArticleRenderContext
{
    public required string                       SourceId   { get; init; }
    public required string                       Identifier { get; init; }
    public required IReadOnlyDictionary<string, string> IdentifierIndex { get; init; }  // identifier -> link
    public required IReadOnlyDictionary<string, string> AssetIndex      { get; init; }  // relative path -> resolved URL
}
