namespace BuildingBlocks.Notifications.Contracts;

public interface INotificationQueue
{
    Task EnqueueAsync(NotificationMessage message, CancellationToken ct = default);
}

public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationMessage message, CancellationToken ct = default);
}

public interface IUserPushTokenStore
{
    Task UpsertAsync(UserPushToken token, CancellationToken ct = default);
    Task<List<UserPushToken>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task DeleteAsync(long userId, string token, CancellationToken ct = default);
}

public interface IOutboxStore
{
    Task AddAsync(string payloadJson, CancellationToken ct = default);

    Task<List<OutboxItem>> DequeueBatchAsync(int batchSize, DateTime utcNow, int maxRetries, CancellationToken ct = default);
    Task MarkProcessingAsync(IEnumerable<long> ids, DateTime utcNow, CancellationToken ct = default);

    Task MarkSentAsync(long id, CancellationToken ct = default);
    Task MarkFailedAsync(long id, int retryCount, DateTime nextAttemptUtc, string error, CancellationToken ct = default);
}

public interface IRealtimeProvider
{
    Task SendAsync(NotificationMessage msg, CancellationToken ct = default);
}

public interface IPushProvider
{
    Task SendAsync(NotificationMessage msg, IReadOnlyList<UserPushToken> tokens, CancellationToken ct = default);
}

public sealed class UserPushToken
{
    public long UserId { get; set; }
    public string Token { get; set; } = default!;
    public PushTokenProvider Provider { get; set; }
    public string? DeviceId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class OutboxItem
{
    public long Id { get; set; }
    public OutboxStatus Status { get; set; }
    public string PayloadJson { get; set; } = default!;
    public int RetryCount { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
}
