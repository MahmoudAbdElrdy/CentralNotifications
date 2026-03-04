using BuildingBlocks.Notifications.Contracts;

namespace BuildingBlocks.Notifications.Outbox;

public sealed class OutboxNotificationQueue : INotificationQueue
{
    private readonly IOutboxStore _store;
    public OutboxNotificationQueue(IOutboxStore store) => _store = store;

    public Task EnqueueAsync(NotificationMessage message, CancellationToken ct = default)
        => _store.AddAsync(NotificationJson.ToJson(message), ct);
}
