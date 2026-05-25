using Encyclopedia.Models;

namespace Encyclopedia.Services.Auth;

/// <summary>
/// Server-side helpers that act on GitHub on behalf of the user. The user's
/// GitHub token is passed in per-call and never persisted server-side.
/// </summary>
public interface IGitHubWorkspaceService
{
    /// <summary>
    /// Validate a PAT: returns the GitHub login it belongs to, the OAuth scopes
    /// granted, and whether those scopes are sufficient to create + write to a
    /// repo. For fine-grained tokens, OauthScopes will be empty; we then probe
    /// by attempting a no-op API and reporting any 403/404.
    /// </summary>
    Task<TokenValidation> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Create a workspace repo for this user if one doesn't exist, initialize
    /// it with a <c>.wiki-meta.yml</c>, replace the README, and apply the
    /// <c>encyclopedia-wiki</c> topic so the discovery service picks it up
    /// automatically. Pass <paramref name="progress"/> to receive a short
    /// status string before each GitHub API call.
    /// </summary>
    Task<WorkspaceRepo> CreateWorkspaceAsync(string token, Account account, IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>Returns true if the user already has a workspace repo we'd recognize.</summary>
    Task<WorkspaceRepo?> FindExistingWorkspaceAsync(string token, string login, CancellationToken ct = default);
}

public sealed record TokenValidation(
    bool                Ok,
    string?             Login,
    string?             Name,
    IReadOnlyList<string> Scopes,
    string?             Error);
