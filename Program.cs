using BlazorBlueprint.Components;
using BlueprintShell;
using BlueprintShell.Shell;
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

    // DI-aware chrome resolution. Anonymous visitors see the bare reader;
    // authenticated contributors get the editor shell only when they're on
    // an /edit/* route, so reader pages stay clean for signed-in users too.
    o.ChromeForServices = (ctx, sp) =>
    {
        var path = ctx.Request.Path.Value ?? "";
        if (!path.StartsWith("/edit", StringComparison.OrdinalIgnoreCase))
            return ShellChromeMode.Hidden;
        var auth = sp.GetRequiredService<IShellAuthContext>();
        return auth.IsAuthenticated ? ShellChromeMode.Full : ShellChromeMode.Minimal;
    };

    // Stack docks below 768px (helps the editor view on phones).
    o.MobileBreakpointPx = 768;
    o.MobileBehavior     = MobileBehavior.Stacked;

    // Diagnostics endpoint at /_shell/diagnostics — Development only.
    o.EnableDiagnostics  = builder.Environment.IsDevelopment();
});

// Auth bridge: lets the shell filter [EditorPanel/ReaderPage(RequiresRole=...)]
// against our client-side account.
builder.Services.AddScoped<IShellAuthContext, ShellAuthContextAdapter>();

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

// Workaround for embedded mode: StaticShellLoader.LoadInto is only invoked
// by EditorServerHost.StartAsync (standalone). Embedded consumers must run
// the scan themselves so [ReaderPage] / [EditorPanel] discovery happens.
// See docs/BlueprintShell-changes.md (0.1.4 follow-up).
{
    var registry = app.Services.GetRequiredService<BlueprintShell.Shell.ShellRegistry>();
    BlueprintShell.Shell.StaticShellLoader.LoadInto(registry);
}

app.MapBlueprintShellPwa(new BlueprintShellPwaOptions
{
    ShortName        = "Encyclopedia",
    Name             = "Encyclopedia",
    ThemeColor       = "#0a0a0a",
    BackgroundColor  = "#ffffff",
    DisplayMode      = "standalone",
    StartUrl         = "/",
    CacheStaticAssets = true,
    Icons =
    {
        new PwaIcon("/icons/icon-192.png", "192x192", "image/png", "any maskable"),
        new PwaIcon("/icons/icon-512.png", "512x512", "image/png", "any maskable"),
    },
});

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.MapRazorComponents<Encyclopedia.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
