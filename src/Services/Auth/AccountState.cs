using System.Text.Json;
using Encyclopedia.Models;
using Microsoft.JSInterop;

namespace Encyclopedia.Services.Auth;

/// <summary>
/// Scoped per-circuit holder of the currently-signed-in account. Persists to
/// browser localStorage via JS interop so a page refresh doesn't sign the user
/// out. The server never reads or writes localStorage; the JS layer does that.
/// </summary>
public sealed class AccountState
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private Account? _current;
    private bool     _hydrated;

    public event Action? Changed;

    public Account? Current        => _current;
    public bool     IsSignedIn     => _current is not null;
    public string?  GithubToken    => _current?.GithubToken;
    public bool     HasWorkspace   => _current?.Workspace is not null;

    /// <summary>
    /// Read localStorage and load the account if present. Safe to call repeatedly;
    /// no-op after the first successful hydration.
    /// </summary>
    public async Task EnsureHydratedAsync(IJSRuntime js)
    {
        if (_hydrated) return;
        _hydrated = true;

        string? json;
        try
        {
            json = await js.InvokeAsync<string?>("encyclopedia.loadAccount");
        }
        catch
        {
            // Pre-render or JS not available yet: try again next call.
            _hydrated = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            _current = JsonSerializer.Deserialize<Account>(json, JsonOpts);
            Changed?.Invoke();
        }
        catch
        {
            await js.InvokeVoidAsync("encyclopedia.clearAccount");
        }
    }

    /// <summary>Set the current account and persist it.</summary>
    public async Task SetAsync(Account account, IJSRuntime js)
    {
        _current  = account;
        _hydrated = true;
        var json = JsonSerializer.Serialize(account, JsonOpts);
        try { await js.InvokeVoidAsync("encyclopedia.saveAccount", json); } catch { /* pre-render */ }
        Changed?.Invoke();
    }

    /// <summary>Update fields on the current account; throws if not signed in.</summary>
    public Task UpdateAsync(Func<Account, Account> mutator, IJSRuntime js)
    {
        if (_current is null) throw new InvalidOperationException("No account loaded.");
        return SetAsync(mutator(_current), js);
    }

    public async Task ClearAsync(IJSRuntime js)
    {
        _current  = null;
        _hydrated = true;
        try { await js.InvokeVoidAsync("encyclopedia.clearAccount"); } catch { }
        Changed?.Invoke();
    }

    public string SerializeForDownload()
    {
        if (_current is null) throw new InvalidOperationException("No account loaded.");
        return JsonSerializer.Serialize(_current, new JsonSerializerOptions(JsonOpts) { WriteIndented = true });
    }
}
