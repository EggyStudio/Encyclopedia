using Encyclopedia.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encyclopedia.Services.Database;

public sealed class EncyclopediaDbContext(DbContextOptions<EncyclopediaDbContext> options)
    : DbContext(options)
{
    public DbSet<WikiSourceEntity>     WikiSources     => Set<WikiSourceEntity>();
    public DbSet<ArticleEntity>        Articles        => Set<ArticleEntity>();
    public DbSet<ArticleVersionEntity> ArticleVersions => Set<ArticleVersionEntity>();
    public DbSet<IdentifierEntity>     Identifiers     => Set<IdentifierEntity>();
    public DbSet<CrossLinkEntity>      CrossLinks      => Set<CrossLinkEntity>();
    public DbSet<TagEntity>            Tags            => Set<TagEntity>();
    public DbSet<CategoryEntity>       Categories      => Set<CategoryEntity>();
    public DbSet<ContributorEntity>    Contributors    => Set<ContributorEntity>();
    public DbSet<AssetEntity>          Assets          => Set<AssetEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<WikiSourceEntity>(e =>
        {
            e.ToTable("wiki_sources");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(200);
            e.Property(x => x.Trust).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MetaJson).HasColumnType("jsonb");
        });

        b.Entity<ArticleEntity>(e =>
        {
            e.ToTable("articles");
            e.HasKey(x => x.Identifier);
            e.Property(x => x.Identifier).HasMaxLength(200);
            e.HasIndex(x => x.SourceId);
            e.Property(x => x.FrontmatterJson).HasColumnType("jsonb");
            // search_vector is maintained by a Postgres trigger; declare unmapped.
            e.Ignore(x => x.SearchSnippet);
        });

        b.Entity<ArticleVersionEntity>(e =>
        {
            e.ToTable("article_versions");
            e.HasKey(x => new { x.Identifier, x.CommitSha });
            e.HasIndex(x => x.Identifier);
            e.HasIndex(x => x.CommittedAt);
        });

        b.Entity<IdentifierEntity>(e =>
        {
            e.ToTable("identifiers");
            e.HasKey(x => x.Slug);
            e.Property(x => x.Slug).HasMaxLength(200);
            e.HasIndex(x => x.ArticleIdentifier);
        });

        b.Entity<CrossLinkEntity>(e =>
        {
            e.ToTable("crosslinks");
            e.HasKey(x => new { x.SourceIdentifier, x.TargetIdentifier });
            e.HasIndex(x => x.TargetIdentifier);
        });

        b.Entity<TagEntity>(e =>
        {
            e.ToTable("article_tags");
            e.HasKey(x => new { x.Identifier, x.Tag });
            e.HasIndex(x => x.Tag);
        });

        b.Entity<CategoryEntity>(e =>
        {
            e.ToTable("article_categories");
            e.HasKey(x => new { x.Identifier, x.Category });
            e.HasIndex(x => x.Category);
        });

        b.Entity<ContributorEntity>(e =>
        {
            e.ToTable("contributors");
            e.HasKey(x => new { x.Identifier, x.GithubLogin });
            e.HasIndex(x => x.GithubLogin);
        });

        b.Entity<AssetEntity>(e =>
        {
            e.ToTable("assets");
            e.HasKey(x => new { x.SourceId, x.RelativePath });
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
        });
    }
}
