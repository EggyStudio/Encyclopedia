using Encyclopedia.Models;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Encyclopedia.Services.Articles;

public sealed class ArticleParserService : IArticleParserService
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoLinks()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseFootnotes()
        .Build();

    private readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public (Frontmatter Front, string Body) Parse(string rawMarkdown)
    {
        if (string.IsNullOrEmpty(rawMarkdown))
            return (Empty(""), "");

        var text = rawMarkdown.Replace("\r\n", "\n");
        if (!text.StartsWith("---\n"))
            return (Empty(""), text);

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
            return (Empty(""), text);

        var yaml = text.Substring(4, end - 4);
        var body = text.Substring(end + 4).TrimStart('\n');

        var raw = _yaml.Deserialize<FrontmatterDto>(yaml) ?? new FrontmatterDto();
        var fm = new Frontmatter
        {
            Identifier = raw.Identifier ?? "",
            Title      = raw.Title      ?? raw.Identifier ?? "Untitled",
            Summary    = raw.Summary,
            Tags       = raw.Tags       ?? [],
            Categories = raw.Categories ?? [],
            Aliases    = raw.Aliases    ?? [],
            Sources    = raw.Sources    ?? [],
            Image      = raw.Image,
            Published  = raw.Published,
            Updated    = raw.Updated,
        };
        return (fm, body);
    }

    public string RenderHtml(string body, ArticleRenderContext ctx)
        => Markdown.ToHtml(body ?? "", _pipeline);

    private static Frontmatter Empty(string id) => new() { Identifier = id, Title = id };

    // YamlDotNet needs writable members for deserialization, so we shape a DTO
    // and copy into the immutable record above.
    private sealed class FrontmatterDto
    {
        public string?   Identifier { get; set; }
        public string?   Title      { get; set; }
        public string?   Summary    { get; set; }
        public string[]? Tags       { get; set; }
        public string[]? Categories { get; set; }
        public string[]? Aliases    { get; set; }
        public string[]? Sources    { get; set; }
        public string?   Image      { get; set; }
        public DateTime? Published  { get; set; }
        public DateTime? Updated    { get; set; }
    }
}
