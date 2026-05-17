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
        // TODO: split on leading ---\n...---\n, deserialize front YAML into Frontmatter,
        // return remainder as body. Tolerate missing frontmatter for draft articles
        // (synthesize Identifier from filename in caller).
        throw new NotImplementedException();
    }

    public string RenderHtml(string body, ArticleRenderContext ctx)
    {
        // TODO: Markdig pipeline + a custom Markdig extension that:
        //   1. Rewrites image/video/file local links via ctx.AssetIndex.
        //   2. Scans rendered text nodes for occurrences of ctx.IdentifierIndex keys
        //      (whole-word, case-insensitive, first occurrence per paragraph) and
        //      wraps them in <a class="crosslink" href="..."> links.
        //   3. Renders [^ref] footnotes Wikipedia-style.
        throw new NotImplementedException();
    }
}
