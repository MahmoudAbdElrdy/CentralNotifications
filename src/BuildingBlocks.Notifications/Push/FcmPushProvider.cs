using BuildingBlocks.Notifications.Contracts;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Notifications.Push;

public sealed class FcmPushProvider : IPushProvider
{
    private readonly IFcmV1Sender _sender;
    private readonly IConfiguration _cfg;

    public FcmPushProvider(IFcmV1Sender sender, IConfiguration cfg)
    {
        _sender = sender;
        _cfg = cfg;
    }

    public async Task SendAsync(NotificationMessage msg, IReadOnlyList<UserPushToken> tokens, CancellationToken ct = default)
    {
        var ttl = msg.TimeToLiveSeconds ?? _cfg.GetValue<int>("Notifications:DefaultTtlSeconds", 60);

        var list = tokens.Where(t =>
            t.Provider == PushTokenProvider.WebFcm ||
            t.Provider == PushTokenProvider.AndroidFcm ||
            t.Provider == PushTokenProvider.AppleFcm).ToList();

        foreach (var t in list)
            await _sender.SendAsync(msg, t.Token, t.Provider, ttl, ct);
    }
}
