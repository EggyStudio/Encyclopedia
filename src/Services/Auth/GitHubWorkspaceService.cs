using System.Text;
using Octokit;
using Account       = Encyclopedia.Models.Account;
using WorkspaceRepo = Encyclopedia.Models.WorkspaceRepo;

namespace Encyclopedia.Services.Auth;

public sealed class GitHubWorkspaceService : IGitHubWorkspaceService
{
    // Topics on GitHub are restricted to lowercase + hyphen, so the discovery
    // topic stays kebab-case. The repo name itself is unconstrained, so we
    // use PascalCase to fit alongside other project repos on the same owner.
    public const string DiscoveryTopic = "encyclopedia-wiki";
    public const string DefaultRepoName = "EncyclopediaWiki";
    public const string MetaFileName    = ".wiki-meta.yml";

    private static readonly string[] RequiredScopes = ["repo"]; // public_repo would be enough for public repos

    private readonly ILogger<GitHubWorkspaceService> _log;

    public GitHubWorkspaceService(ILogger<GitHubWorkspaceService> log) => _log = log;

    private static GitHubClient ClientFor(string token)
    {
        var c = new GitHubClient(new ProductHeaderValue("Encyclopedia"));
        c.Credentials = new Credentials(token);
        return c;
    }

    public async Task<TokenValidation> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new TokenValidation(false, null, null, [], "Token is empty.");

        var gh = ClientFor(token);
        try
        {
            var user = await gh.User.Current();
            var scopes = gh.Connection.GetLastApiInfo()?.OauthScopes ?? [];
            // Classic PAT exposes scopes; fine-grained tokens return an empty list.
            // We don't reject fine-grained here - we'll let the repo-create call surface
            // any permission failure with a clearer message at that point.
            return new TokenValidation(true, user.Login, user.Name, scopes, null);
        }
        catch (AuthorizationException ex)
        {
            return new TokenValidation(false, null, null, [], $"Token rejected by GitHub ({ex.StatusCode}).");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "ValidateTokenAsync failed");
            return new TokenValidation(false, null, null, [], ex.Message);
        }
    }

    public async Task<WorkspaceRepo?> FindExistingWorkspaceAsync(string token, string login, CancellationToken ct = default)
    {
        var gh = ClientFor(token);
        // Try the canonical name first.
        var candidate = await TryGetAsync(gh, login, DefaultRepoName);
        if (candidate is not null && await HasEncyclopediaMetaAsync(gh, candidate)) return ToWorkspace(candidate);

        // Otherwise, walk the user's repos and look for the topic.
        var repos = await gh.Repository.GetAllForCurrent(new RepositoryRequest
        {
            Affiliation = RepositoryAffiliation.Owner,
            Sort        = RepositorySort.Updated,
        });
        foreach (var r in repos)
        {
            var topics = await gh.Repository.GetAllTopics(r.Id);
            if (topics.Names.Contains(DiscoveryTopic) && await HasEncyclopediaMetaAsync(gh, r))
                return ToWorkspace(r);
        }
        return null;
    }

    public async Task<WorkspaceRepo> CreateWorkspaceAsync(string token, Account account, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var gh = ClientFor(token);

        progress?.Report("Checking your GitHub account…");
        var user = await gh.User.Current();
        var login = user.Login;

        progress?.Report("Looking for an existing workspace…");
        var existing = await FindExistingWorkspaceAsync(token, login, ct);
        if (existing is not null) return existing;

        // Pick a name. If `EncyclopediaWiki` is taken, suffix with -1, -2, ...
        progress?.Report("Picking a repository name…");
        var repoName = await PickAvailableNameAsync(gh, login, DefaultRepoName);

        var newRepo = new NewRepository(repoName)
        {
            Description    = $"Articles by {account.DisplayName} for the Encyclopedia.",
            AutoInit       = true,           // gives us a main branch + initial commit
            Private        = false,
            HasIssues      = true,
            HasWiki        = false,
            HasDownloads   = false,
            LicenseTemplate = "cc-by-4.0",
        };
        progress?.Report($"Creating {login}/{repoName} on GitHub…");
        var created = await gh.Repository.Create(newRepo);
        var branch  = string.IsNullOrEmpty(created.DefaultBranch) ? "main" : created.DefaultBranch;
        var identifier = SlugifyIdentifier(login);

        // .wiki-meta.yml ---------------------------------------------------------
        // Empty articles/ and assets/ trees aren't scaffolded - GitHub's contents
        // API (and Octokit) reject zero-byte files, and the directories will
        // materialize when the first article / asset commit lands anyway.
        progress?.Report("Writing .wiki-meta.yml…");
        var meta = BuildMetaYaml(identifier, account, login);
        await gh.Repository.Content.CreateFile(login, repoName, MetaFileName,
            new CreateFileRequest(
                message: "chore: initialize encyclopedia workspace",
                content: meta,
                branch:  branch));

        // README ----------------------------------------------------------------
        progress?.Report("Replacing the README…");
        var readme = BuildReadme(account, login, identifier);
        await gh.Repository.Content.UpdateFile(login, repoName, "README.md",
            new UpdateFileRequest(
                message: "docs: replace generated README",
                content: readme,
                sha:     (await gh.Repository.Content.GetAllContents(login, repoName, "README.md"))[0].Sha,
                branch:  branch));

        // Topic so discovery picks it up ---------------------------------------
        progress?.Report("Applying the encyclopedia-wiki topic…");
        await gh.Repository.ReplaceAllTopics(login, repoName, new RepositoryTopics(new[] { DiscoveryTopic }));

        return new WorkspaceRepo
        {
            Owner         = login,
            Repo          = repoName,
            DefaultBranch = branch,
            Identifier    = identifier,
            CreatedAt     = DateTime.UtcNow,
            HtmlUrl       = created.HtmlUrl,
        };
    }

    // ----- helpers ---------------------------------------------------------

    private static async Task<Repository?> TryGetAsync(IGitHubClient gh, string owner, string name)
    {
        try { return await gh.Repository.Get(owner, name); }
        catch (NotFoundException) { return null; }
    }

    private static async Task<bool> HasEncyclopediaMetaAsync(IGitHubClient gh, Repository r)
    {
        try
        {
            var contents = await gh.Repository.Content.GetAllContents(r.Owner.Login, r.Name, MetaFileName);
            return contents.Count > 0;
        }
        catch (NotFoundException) { return false; }
    }

    private static async Task<string> PickAvailableNameAsync(IGitHubClient gh, string owner, string baseName)
    {
        if (await TryGetAsync(gh, owner, baseName) is null) return baseName;
        for (var i = 1; i < 50; i++)
        {
            var n = $"{baseName}-{i}";
            if (await TryGetAsync(gh, owner, n) is null) return n;
        }
        throw new InvalidOperationException("Couldn't find an available workspace repo name.");
    }

    private static WorkspaceRepo ToWorkspace(Repository r) => new()
    {
        Owner         = r.Owner.Login,
        Repo          = r.Name,
        DefaultBranch = string.IsNullOrEmpty(r.DefaultBranch) ? "main" : r.DefaultBranch,
        Identifier    = SlugifyIdentifier(r.Owner.Login),
        CreatedAt     = r.CreatedAt.UtcDateTime,
        HtmlUrl       = r.HtmlUrl,
    };

    private static string SlugifyIdentifier(string login)
    {
        var sb = new StringBuilder(login.Length);
        foreach (var c in login.ToLowerInvariant())
            sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '-');
        return sb.ToString().Trim('-');
    }

    private static string BuildMetaYaml(string identifier, Account account, string login)
    {
        var title = $"{account.DisplayName}'s Encyclopedia";
        return $"""
            # Auto-generated by the Encyclopedia web app.
            # Edit at any time; the wiki picks up changes on the next discovery sync.
            identifier:  {identifier}
            title:       {title.Replace("\"", "\\\"")}
            description: Articles contributed by {account.DisplayName}.
            owner:       {login}
            language:    en
            tags:        []
            categories:  []
            articlesDir: articles
            assetsDir:   assets
            assets:      github
            """;
    }

    private static string BuildReadme(Account account, string login, string identifier) => $"""
        # {account.DisplayName}'s Encyclopedia workspace

        This repository was created automatically by the
        [Encyclopedia](https://github.com/) web app and is the home for
        articles authored by **@{login}**.

        - Articles live under `articles/` as Markdown with YAML frontmatter.
        - Assets live under `assets/`.
        - `.wiki-meta.yml` declares this repo to the encyclopedia.
        - The `encyclopedia-wiki` topic makes it discoverable.

        You don't need to edit anything here by hand - use the Encyclopedia
        web app's editor. Identifier: `{identifier}`.
        """;
}
