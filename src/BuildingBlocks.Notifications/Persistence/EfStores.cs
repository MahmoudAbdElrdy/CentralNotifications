using BuildingBlocks.Notifications.Contracts;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Notifications.Persistence;

public sealed class EfUserPushTokenStore : IUserPushTokenStore
{
    private readonly NotificationsDbContext _db;
    public EfUserPushTokenStore(NotificationsDbContext db) => _db = db;

    public async Task UpsertAsync(UserPushToken token, CancellationToken ct = default)
    {
        var deviceKey = token.DeviceId ?? "";
        var existing = await _db.UserPushTokens.FirstOrDefaultAsync(x =>
            x.UserId == token.UserId &&
            x.Provider == token.Provider &&
            (x.DeviceId ?? "") == deviceKey, ct);

        if (existing is null)
        {
            _db.UserPushTokens.Add(new UserPushTokenEntity
            {
                UserId = token.UserId,
                Token = token.Token,
                Provider = token.Provider,
                DeviceId = token.DeviceId,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Token = token.Token;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<UserPushToken>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        var list = await _db.UserPushTokens.Where(x => x.UserId == userId).ToListAsync(ct);
        return list.Select(x => new UserPushToken
        {
            UserId = x.UserId,
            Token = x.Token,
            Provider = x.Provider,
            DeviceId = x.DeviceId,
            UpdatedAtUtc = x.UpdatedAtUtc
        }).ToList();
    }

    public async Task DeleteAsync(long userId, string token, CancellationToken ct = default)
    {
        var rows = await _db.UserPushTokens.Where(x => x.UserId == userId && x.Token == token).ToListAsync(ct);
        _db.UserPushTokens.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed class EfOutboxStore : IOutboxStore
{
    private readonly NotificationsDbContext _db;
    public EfOutboxStore(NotificationsDbContext db) => _db = db;

    public async Task AddAsync(string payloadJson, CancellationToken ct = default)
    {
        _db.Outbox.Add(new OutboxEntity
        {
            Status = OutboxStatus.Pending,
            PayloadJson = payloadJson,
            RetryCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<OutboxItem>> DequeueBatchAsync(int batchSize, DateTime utcNow, int maxRetries, CancellationToken ct = default)
    {
        var items = await _db.Outbox
            .Where(x => (x.Status == OutboxStatus.Pending || x.Status == OutboxStatus.Failed) &&
                        x.NextAttemptAtUtc <= utcNow &&
                        x.RetryCount < maxRetries)
            .OrderBy(x => x.NextAttemptAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        return items.Select(x => new OutboxItem
        {
            Id = x.Id,
            Status = x.Status,
            PayloadJson = x.PayloadJson,
            RetryCount = x.RetryCount,
            NextAttemptAtUtc = x.NextAttemptAtUtc
        }).ToList();
    }

    public async Task MarkProcessingAsync(IEnumerable<long> ids, DateTime utcNow, CancellationToken ct = default)
    {
        var items = await _db.Outbox.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        foreach (var it in items)
        {
            it.Status = OutboxStatus.Processing;
            it.LastAttemptAtUtc = utcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkSentAsync(long id, CancellationToken ct = default)
    {
        var it = await _db.Outbox.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (it is null) return;
        it.Status = OutboxStatus.Sent;
        it.LastError = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(long id, int retryCount, DateTime nextAttemptUtc, string error, CancellationToken ct = default)
    {
        var it = await _db.Outbox.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (it is null) return;
        it.Status = OutboxStatus.Failed;
        it.RetryCount = retryCount;
        it.NextAttemptAtUtc = nextAttemptUtc;
        it.LastError = error;
        await _db.SaveChangesAsync(ct);
    }
}
