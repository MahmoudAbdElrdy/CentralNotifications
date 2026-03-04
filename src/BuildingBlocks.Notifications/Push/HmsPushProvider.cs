using BuildingBlocks.Notifications.Contracts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using HMS = AGConnectAdmin.Messaging;

namespace BuildingBlocks.Notifications.Push;

public sealed class HmsPushProvider : IPushProvider
{
    private readonly IHmsMessagingFactory _factory;
    private readonly IConfiguration _cfg;

    public HmsPushProvider(IHmsMessagingFactory factory, IConfiguration cfg)
    {
        _factory = factory;
        _cfg = cfg;
    }

    public async Task SendAsync(NotificationMessage msg, IReadOnlyList<UserPushToken> tokens, CancellationToken ct = default)
    {
        var ttl = msg.TimeToLiveSeconds ?? _cfg.GetValue<int>("Notifications:DefaultTtlSeconds", 60);

        var list = tokens.Where(t =>
            t.Provider == PushTokenProvider.HuaweiUser ||
            t.Provider == PushTokenProvider.HuaweiDriver ||
            t.Provider == PushTokenProvider.HuaweiMerchant).ToList();

        foreach (var t in list)
        {
            var client = _factory.Get(t.Provider);

            var message = new HMS.Message
            {
                Token = new List<string> { t.Token },
                Data = JsonConvert.SerializeObject(msg.Data ?? new { }),
                Android = new HMS.AndroidConfig { TTL = TimeSpan.FromSeconds(ttl), Urgency = HMS.UrgencyPriority.HIGH }
            };

            if (!msg.Silent)
            {
                message.Notification = new HMS.Notification { Title = msg.Title, Body = msg.Body };
                message.Android.Notification = new HMS.AndroidNotification
                {
                    Title = msg.Title,
                    Body = msg.Body,
                    ClickAction = HMS.ClickAction.OpenApp(),
                    Importance = HMS.NotificationImportance.HIGH,
                    Sound = "default"
                };
            }

            await client.SendAsync(message);
        }
    }
}
