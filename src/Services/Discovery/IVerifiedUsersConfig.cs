namespace Encyclopedia.Services.Discovery;

public interface IVerifiedUsersConfig
{
    /// <summary>GitHub logins whose repos are automatically Verified.</summary>
    IReadOnlySet<string> VerifiedOwners { get; }

    /// <summary>Explicit <c>owner/repo</c> entries that bypass owner-level trust.</summary>
    IReadOnlySet<string> VerifiedRepos { get; }
}
