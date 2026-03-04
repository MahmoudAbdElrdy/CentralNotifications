using BuildingBlocks.Notifications.Contracts;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

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

            var message = new HmsMessageRequest
            {
                ValidateOnly = false,
                Message = new HmsMessage
                {
                    Token = new List<string> { t.Token },
                    Data = JsonConvert.SerializeObject(msg.Data ?? new { }),
                    Android = new HmsAndroidConfig
                    {
                        Ttl = $"{ttl}s",
                        Urgency = "HIGH"
                    }
                }
            };

            if (!msg.Silent)
            {
                message.Message.Notification = new HmsNotification
                {
                    Title = msg.Title,
                    Body = msg.Body
                };
                message.Message.Android.Notification = new HmsAndroidNotification
                {
                    Title = msg.Title,
                    Body = msg.Body,
                    ClickAction = new HmsClickAction { Type = 1 }, // 1 = Open App
                    Importance = "HIGH",
                    Sound = "default"
                };
            }

            await client.SendAsync(message, ct);
        }
    }
}
