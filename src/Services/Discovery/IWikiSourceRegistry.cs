using Encyclopedia.Models;

namespace Encyclopedia.Services.Discovery;

public interface IWikiSourceRegistry
{
    Task<IReadOnlyList<WikiSource>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WikiSource>> GetByTrustAsync(WikiSourceTrust trust, CancellationToken ct = default);
    Task<WikiSource?>               FindAsync(string ownerSlashRepo, CancellationToken ct = default);

    Task UpsertAsync(WikiSource source, CancellationToken ct = default);
    Task SetTrustAsync(string ownerSlashRepo, WikiSourceTrust trust, CancellationToken ct = default);
}
