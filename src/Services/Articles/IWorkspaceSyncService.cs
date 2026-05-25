using Encyclopedia.Models;

namespace Encyclopedia.Services.Articles;

/// <summary>
/// Pulls a workspace's articles from GitHub and upserts them into the local
/// Postgres database so they appear on Discover / Home / /wiki/... pages.
/// Called after a successful article publish, or manually from the Profile
/// "Sync now" button.
/// </summary>
public interface IWorkspaceSyncService
{
    Task<SyncResult> SyncWorkspaceAsync(
        Account account,
        WorkspaceRepo workspace,
        IProgress<string>? progress = null,
        CancellationToken ct = default);
}

public sealed record SyncResult(int Upserted, int Removed, int Total);
