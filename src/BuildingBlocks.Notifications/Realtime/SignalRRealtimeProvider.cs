using BuildingBlocks.Notifications.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace BuildingBlocks.Notifications.Realtime;

public sealed class SignalRRealtimeProvider : IRealtimeProvider
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRRealtimeProvider(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task SendAsync(NotificationMessage msg, CancellationToken ct = default)
    {
        var json = NotificationJson.ToJson(msg);
        return msg.TargetType switch
        {
            NotificationTargetType.User => _hub.Clients.Group($"user:{msg.Target}").SendAsync("ReceiveNotification", json, ct),
            NotificationTargetType.Group => _hub.Clients.Group($"group:{msg.Target}").SendAsync("ReceiveNotification", json, ct),
            NotificationTargetType.Topic => _hub.Clients.Group($"topic:{msg.Target}").SendAsync("ReceiveNotification", json, ct),
            _ => Task.CompletedTask
        };
    }
}
