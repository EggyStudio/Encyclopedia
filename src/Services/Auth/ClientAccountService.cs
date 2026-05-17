using System.Text.Json;
using Encyclopedia.Models;

namespace Encyclopedia.Services.Auth;

public sealed class ClientAccountService : IClientAccountService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Account Create(string displayName, string? email) => new()
    {
        Id          = Guid.NewGuid().ToString("n"),
        DisplayName = displayName,
        Email       = email,
    };

    public string Serialize(Account account) => JsonSerializer.Serialize(account, JsonOpts);

    public Account Deserialize(string json) =>
        JsonSerializer.Deserialize<Account>(json, JsonOpts)
        ?? throw new FormatException("Account file could not be parsed.");

    public bool Validate(Account account, out string? error)
    {
        if (string.IsNullOrWhiteSpace(account.Id))          { error = "Missing id";          return false; }
        if (string.IsNullOrWhiteSpace(account.DisplayName)) { error = "Missing displayName"; return false; }
        if (account.SchemaVersion != 1)                     { error = "Unsupported schemaVersion"; return false; }
        error = null;
        return true;
    }
}
