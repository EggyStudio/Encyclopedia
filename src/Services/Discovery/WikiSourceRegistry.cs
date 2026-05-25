using System.Text.Json;
using Encyclopedia.Models;
using Encyclopedia.Services.Database;
using Encyclopedia.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Encyclopedia.Services.Discovery;

public sealed class WikiSourceRegistry : IWikiSourceRegistry
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly EncyclopediaDbContext _db;
    private readonly IVerifiedUsersConfig  _verified;

    public WikiSourceRegistry(EncyclopediaDbContext db, IVerifiedUsersConfig verified)
    {
        _db       = db;
        _verified = verified;
    }

    public async Task<IReadOnlyList<WikiSource>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.WikiSources.AsNoTracking().ToListAsync(ct);
        return rows.Select(ToModel).ToList();
    }

    public async Task<IReadOnlyList<WikiSource>> GetByTrustAsync(WikiSourceTrust trust, CancellationToken ct = default)
    {
        var rows = await _db.WikiSources.AsNoTracking().Where(s => s.Trust == trust).ToListAsync(ct);
        return rows.Select(ToModel).ToList();
    }

    public async Task<WikiSource?> FindAsync(string ownerSlashRepo, CancellationToken ct = default)
    {
        var row = await _db.WikiSources.AsNoTracking().FirstOrDefaultAsync(s => s.Id == ownerSlashRepo, ct);
        return row is null ? null : ToModel(row);
    }

    public async Task UpsertAsync(WikiSource source, CancellationToken ct = default)
    {
        var existing = await _db.WikiSources.FirstOrDefaultAsync(s => s.Id == source.Id, ct);
        var metaJson = JsonSerializer.Serialize(source.Meta, JsonOpts);

        if (existing is null)
        {
            _db.WikiSources.Add(new WikiSourceEntity
            {
                Id            = source.Id,
                Owner         = source.Owner,
                Repo          = source.Repo,
                DefaultBranch = source.DefaultBranch,
                Trust         = ApplyVerifiedOverlay(source),
                MetaJson      = metaJson,
                LastSyncedAt  = source.LastSyncedAt == default ? DateTime.UtcNow : source.LastSyncedAt,
                LastSyncedSha = source.LastSyncedSha,
            });
        }
        else
        {
            existing.Owner         = source.Owner;
            existing.Repo          = source.Repo;
            existing.DefaultBranch = source.DefaultBranch;
            existing.Trust         = ApplyVerifiedOverlay(source);
            existing.MetaJson      = metaJson;
            existing.LastSyncedAt  = source.LastSyncedAt == default ? DateTime.UtcNow : source.LastSyncedAt;
            existing.LastSyncedSha = source.LastSyncedSha;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetTrustAsync(string ownerSlashRepo, WikiSourceTrust trust, CancellationToken ct = default)
    {
        var row = await _db.WikiSources.FirstOrDefaultAsync(s => s.Id == ownerSlashRepo, ct);
        if (row is null) return;
        row.Trust = trust;
        await _db.SaveChangesAsync(ct);
    }

    // verified-users.yml overlay: an owner / owner-repo listed there is always
    // surfaced as Verified, regardless of how the row was first inserted.
    private WikiSourceTrust ApplyVerifiedOverlay(WikiSource source)
    {
        if (_verified.VerifiedOwners.Contains(source.Owner)) return WikiSourceTrust.Verified;
        if (_verified.VerifiedRepos.Contains(source.Id))     return WikiSourceTrust.Verified;
        return source.Trust;
    }

    private static WikiSource ToModel(WikiSourceEntity e)
    {
        WikiMeta meta;
        try { meta = JsonSerializer.Deserialize<WikiMeta>(e.MetaJson, JsonOpts) ?? StubMeta(e); }
        catch { meta = StubMeta(e); }

        return new WikiSource
        {
            Id            = e.Id,
            Owner         = e.Owner,
            Repo          = e.Repo,
            DefaultBranch = e.DefaultBranch,
            Trust         = e.Trust,
            Meta          = meta,
            LastSyncedAt  = e.LastSyncedAt,
            LastSyncedSha = e.LastSyncedSha,
        };
    }

    private static WikiMeta StubMeta(WikiSourceEntity e) => new()
    {
        Identifier = e.Owner.ToLowerInvariant(),
        Title      = $"{e.Owner}/{e.Repo}",
        Owner      = e.Owner,
    };
}
