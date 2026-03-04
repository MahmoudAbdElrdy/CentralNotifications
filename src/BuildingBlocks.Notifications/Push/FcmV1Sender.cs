using System.Net.Http.Headers;
using BuildingBlocks.Notifications.Contracts;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BuildingBlocks.Notifications.Push;

public sealed class FcmV1Options
{
    public string ProjectId { get; set; } = default!;
    public string ServiceAccountJsonPath { get; set; } = default!;
}

public interface IFcmV1Sender
{
    Task SendAsync(NotificationMessage msg, string token, PushTokenProvider provider, int ttlSeconds, CancellationToken ct);
}

public sealed class FcmV1Sender : IFcmV1Sender
{
    private static readonly string[] Scopes = { "https://www.googleapis.com/auth/firebase.messaging" };

    private readonly HttpClient _http;
    private readonly FcmV1Options _opt;

    public FcmV1Sender(HttpClient http, IOptions<FcmV1Options> opt)
    {
        _http = http;
        _opt = opt.Value;
    }

    public async Task SendAsync(NotificationMessage msg, string token, PushTokenProvider provider, int ttlSeconds, CancellationToken ct)
    {
        var accessToken = await GetAccessTokenAsync(ct);

        var data = new Dictionary<string, string>
        {
            ["label"] = msg.Label ?? "",
            ["payload"] = JsonConvert.SerializeObject(msg.Data ?? new { })
        };

        var root = new JObject
        {
            ["message"] = new JObject
            {
                ["token"] = token,
                ["data"] = JObject.FromObject(data),
            }
        };

        if (!msg.Silent)
        {
            root["message"]!["notification"] = new JObject
            {
                ["title"] = msg.Title ?? "",
                ["body"] = msg.Body ?? ""
            };
        }

        if (provider == PushTokenProvider.WebFcm)
        {
            root["message"]!["webpush"] = new JObject
            {
                ["headers"] = new JObject { ["TTL"] = ttlSeconds.ToString() },
                ["notification"] = new JObject { ["tag"] = string.IsNullOrWhiteSpace(msg.Label) ? "general" : msg.Label }
            };
        }
        else
        {
            root["message"]!["android"] = new JObject { ["ttl"] = $"{ttlSeconds}s", ["priority"] = "HIGH" };
        }

        var url = $"https://fcm.googleapis.com/v1/projects/{_opt.ProjectId}/messages:send";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = new StringContent(root.ToString(Newtonsoft.Json.Formatting.None));
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new Exception($"FCM v1 failed: {(int)res.StatusCode} - {body}");
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var cred = GoogleCredential.FromFile(_opt.ServiceAccountJsonPath).CreateScoped(Scopes);
        return await cred.UnderlyingCredential.GetAccessTokenForRequestAsync(null, ct);
    }
}
