using BuildingBlocks.Notifications.Contracts;

namespace BuildingBlocks.Notifications.Core;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IEnumerable<IRealtimeProvider> _realtime;
    private readonly IEnumerable<IPushProvider> _push;
    private readonly IUserPushTokenStore _tokens;

    public NotificationDispatcher(IEnumerable<IRealtimeProvider> realtime, IEnumerable<IPushProvider> push, IUserPushTokenStore tokens)
    {
        _realtime = realtime;
        _push = push;
        _tokens = tokens;
    }

    public async Task DispatchAsync(NotificationMessage msg, CancellationToken ct = default)
    {
        foreach (var r in _realtime)
        {
            try { await r.SendAsync(msg, ct); }
            catch { }
        }

        if (msg.TargetType != NotificationTargetType.User) return;
        if (!long.TryParse(msg.Target, out var userId)) return;

        var tokens = await _tokens.GetByUserIdAsync(userId, ct);
        if (tokens.Count == 0) return;

        foreach (var p in _push)
            await p.SendAsync(msg, tokens, ct);
    }
}
