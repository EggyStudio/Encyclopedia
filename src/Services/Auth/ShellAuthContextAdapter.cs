using BlueprintShell.Shell;

namespace Encyclopedia.Services.Auth;

/// <summary>
/// Bridges our client-side <see cref="AccountState"/> into the shell's
/// <see cref="IShellAuthContext"/>. The shell consults this when deciding
/// whether to render panels / reader pages marked <c>RequiresRole = "..."</c>.
///
/// Roles surfaced:
///   - "*"          when any account is loaded (covered by <c>IsAuthenticated</c>)
///   - "contributor" when a workspace repo exists on the account
///   - "github"     when a GitHub token is connected
/// </summary>
public sealed class ShellAuthContextAdapter : IShellAuthContext
{
    private readonly AccountState _state;

    public ShellAuthContextAdapter(AccountState state) => _state = state;

    public bool IsAuthenticated => _state.IsSignedIn;
    public string? UserId       => _state.Current?.Id;

    public IReadOnlySet<string> Roles
    {
        get
        {
            var roles = new HashSet<string>(StringComparer.Ordinal);
            var account = _state.Current;
            if (account is null) return roles;
            if (!string.IsNullOrEmpty(account.GithubToken)) roles.Add("github");
            if (account.Workspace is not null)              roles.Add("contributor");
            return roles;
        }
    }
}
