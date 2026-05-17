using BlazorBlueprint.Components;
using BlueprintShell;
using Encyclopedia.Components;
using Encyclopedia.Services.Articles;
using Encyclopedia.Services.Assets;
using Encyclopedia.Services.Auth;
using Encyclopedia.Services.Database;
using Encyclopedia.Services.Discovery;
using Encyclopedia.Services.Mobile;
using Encyclopedia.Services.Search;
using Encyclopedia.Services.Versioning;
using Microsoft.EntityFrameworkCore;
using Octokit;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Blazor + BlueprintShell
// -----------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();

builder.Services.AddBlueprintShell(o =>
{
    o.AppTitle = "Encyclopedia";
});

builder.Services.AddBlazorBlueprintComponents();

// -----------------------------------------------------------------------------
// Database
// -----------------------------------------------------------------------------
var connString = builder.Configuration.GetConnectionString("Postgres")
                 ?? "Host=localhost;Port=5432;Database=encyclopedia;Username=encyclopedia;Password=encyclopedia";
builder.Services.AddDbContextPool<EncyclopediaDbContext>(o => o.UseNpgsql(connString));

// -----------------------------------------------------------------------------
// External clients
// -----------------------------------------------------------------------------
builder.Services.AddSingleton<IGitHubClient>(_ =>
{
    var client = new GitHubClient(new ProductHeaderValue("Encyclopedia"));
    var token  = builder.Configuration["GITHUB_TOKEN"];
    if (!string.IsNullOrWhiteSpace(token))
        client.Credentials = new Credentials(token);
    return client;
});

// -----------------------------------------------------------------------------
// Encyclopedia services
// -----------------------------------------------------------------------------
var verifiedYamlPath = Path.Combine(builder.Environment.ContentRootPath, "config", "verified-users.yml");
builder.Services.AddSingleton<IVerifiedUsersConfig>(_ => new VerifiedUsersConfig(verifiedYamlPath));

builder.Services.AddScoped<IGitHubRepoDiscoveryService, GitHubRepoDiscoveryService>();
builder.Services.AddScoped<IWikiSourceRegistry,        WikiSourceRegistry>();
builder.Services.AddScoped<IArticleFetchService,       ArticleFetchService>();
builder.Services.AddSingleton<IArticleParserService,   ArticleParserService>();
builder.Services.AddScoped<ICrossLinkIndexService,     CrossLinkIndexService>();
builder.Services.AddSingleton<IGitHubAssetProvider,    GitHubAssetProvider>();
builder.Services.AddSingleton<ICloudflareR2AssetProvider, CloudflareR2AssetProvider>();
builder.Services.AddScoped<IAssetResolver,             AssetResolver>();
builder.Services.AddScoped<IFullTextSearchService,     FullTextSearchService>();
builder.Services.AddScoped<IVersionHistoryService,     VersionHistoryService>();
builder.Services.AddSingleton<IClientAccountService,   ClientAccountService>();
builder.Services.AddScoped<IGitHubPushService,         GitHubPushService>();
builder.Services.AddScoped<IGitHubWorkspaceService,    GitHubWorkspaceService>();
builder.Services.AddScoped<AccountState>();
builder.Services.AddSingleton<IDeviceDetectionService, DeviceDetectionService>();

// -----------------------------------------------------------------------------
// HTTP forwarding (fly.io places us behind a TLS-terminating proxy)
// -----------------------------------------------------------------------------
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapBlueprintShell();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.MapRazorComponents<Encyclopedia.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
