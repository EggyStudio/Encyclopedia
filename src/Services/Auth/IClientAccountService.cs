using Encyclopedia.Models;

namespace Encyclopedia.Services.Auth;

/// <summary>
/// Accounts are client-side: the user generates one in-browser, downloads it as
/// a JSON file (and keeps it themselves), re-uploads it on later sessions.
/// The server never persists Account contents.
/// </summary>
public interface IClientAccountService
{
    Account Create(string displayName, string? email);
    string  Serialize(Account account);
    Account Deserialize(string json);
    bool    Validate(Account account, out string? error);
}
