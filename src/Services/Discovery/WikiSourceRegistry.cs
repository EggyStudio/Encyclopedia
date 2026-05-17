using Encyclopedia.Models;
using Encyclopedia.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace Encyclopedia.Services.Discovery;

public sealed class WikiSourceRegistry : IWikiSourceRegistry
{
    private readonly EncyclopediaDbContext _db;
    private readonly IVerifiedUsersConfig  _verified;

    public WikiSourceRegistry(EncyclopediaDbContext db, IVerifiedUsersConfig verified)
    {
        _db       = db;
        _verified = verified;
    }

    public Task<IReadOnlyList<WikiSource>> GetAllAsync(CancellationToken ct = default)
    {
        // TODO: query db for sources, apply verified-users overlay so that
        // an entry listed in verified-users.yml is always Verified regardless
        // of stored trust.
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<WikiSource>> GetByTrustAsync(WikiSourceTrust trust, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<WikiSource?> FindAsync(string ownerSlashRepo, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task UpsertAsync(WikiSource source, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task SetTrustAsync(string ownerSlashRepo, WikiSourceTrust trust, CancellationToken ct = default)
        => throw new NotImplementedException();
}
