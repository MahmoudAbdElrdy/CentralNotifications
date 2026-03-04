using System.Collections.Concurrent;
using AGConnectAdmin;
using BuildingBlocks.Notifications.Contracts;
using Microsoft.Extensions.Options;
using HMS = AGConnectAdmin.Messaging;

namespace BuildingBlocks.Notifications.Push;

public sealed class HuaweiPushOptions
{
    public string LoginUri { get; set; } = default!;
    public string ApiBaseUri { get; set; } = default!;
    public Dictionary<string, HuaweiAppOptions> Apps { get; set; } = new();
}

public sealed class HuaweiAppOptions
{
    public string AppInstanceName { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
}

public interface IHmsMessagingFactory
{
    HMS.AGConnectMessaging Get(PushTokenProvider provider);
}

public sealed class HmsMessagingFactory : IHmsMessagingFactory
{
    private readonly HuaweiPushOptions _opt;
    private readonly ConcurrentDictionary<PushTokenProvider, HMS.AGConnectMessaging> _cache = new();

    public HmsMessagingFactory(IOptions<HuaweiPushOptions> opt) => _opt = opt.Value;

    public HMS.AGConnectMessaging Get(PushTokenProvider provider) => _cache.GetOrAdd(provider, Create);

    private HMS.AGConnectMessaging Create(PushTokenProvider provider)
    {
        var key = provider.ToString();
        if (!_opt.Apps.TryGetValue(key, out var appCfg))
            throw new InvalidOperationException($"HuaweiPush:Apps:{key} not configured.");

        var app = AGConnectApp.Create(new AppOptions
        {
            LoginUri = _opt.LoginUri,
            ApiBaseUri = _opt.ApiBaseUri,
            ClientId = appCfg.ClientId,
            ClientSecret = appCfg.ClientSecret
        }, appCfg.AppInstanceName);

        return HMS.AGConnectMessaging.GetMessaging(app);
    }
}
