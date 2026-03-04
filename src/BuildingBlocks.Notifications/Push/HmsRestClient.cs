using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BuildingBlocks.Notifications.Contracts;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Notifications.Push;

public sealed class HmsAuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = default!;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = default!;
}

public sealed class HmsMessageRequest
{
    [JsonPropertyName("validate_only")]
    public bool ValidateOnly { get; set; }

    [JsonPropertyName("message")]
    public HmsMessage Message { get; set; } = default!;
}

public sealed class HmsMessage
{
    [JsonPropertyName("token")]
    public List<string>? Token { get; set; }

    [JsonPropertyName("notification")]
    public HmsNotification? Notification { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("android")]
    public HmsAndroidConfig? Android { get; set; }
}

public sealed class HmsNotification
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("body")]
    public string Body { get; set; } = default!;
}

public sealed class HmsAndroidConfig
{
    [JsonPropertyName("ttl")]
    public string? Ttl { get; set; }

    [JsonPropertyName("urgency")]
    public string? Urgency { get; set; }

    [JsonPropertyName("notification")]
    public HmsAndroidNotification? Notification { get; set; }
}

public sealed class HmsAndroidNotification
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("click_action")]
    public HmsClickAction? ClickAction { get; set; }

    [JsonPropertyName("sound")]
    public string? Sound { get; set; }

    [JsonPropertyName("importance")]
    public string? Importance { get; set; }
}

public sealed class HmsClickAction
{
    [JsonPropertyName("type")]
    public int Type { get; set; }
}

public interface IHmsRestClient
{
    Task<string> GetAccessTokenAsync(string clientId, string clientSecret, CancellationToken ct = default);
    Task SendMessageAsync(string appId, string accessToken, HmsMessageRequest message, CancellationToken ct = default);
}

public sealed class HmsRestClient : IHmsRestClient
{
    private readonly HttpClient _httpClient;
    private readonly HuaweiPushOptions _options;
    private readonly Dictionary<string, (string Token, DateTime Expiry)> _tokenCache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public HmsRestClient(HttpClient httpClient, IOptions<HuaweiPushOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, CancellationToken ct = default)
    {
        var cacheKey = clientId;
        
        await _lock.WaitAsync(ct);
        try
        {
            // Check cache
            if (_tokenCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow.AddMinutes(5))
            {
                return cached.Token;
            }

            // Request new token
            var loginUri = _options.LoginUri.TrimEnd('/');
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
            });

            var response = await _httpClient.PostAsync($"{loginUri}/oauth2/v3/token", content, ct);
            response.EnsureSuccessStatusCode();

            var authResponse = await response.Content.ReadFromJsonAsync<HmsAuthResponse>(ct);
            if (authResponse == null)
                throw new InvalidOperationException("Failed to get HMS access token");

            var expiry = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn);
            _tokenCache[cacheKey] = (authResponse.AccessToken, expiry);

            return authResponse.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SendMessageAsync(string appId, string accessToken, HmsMessageRequest message, CancellationToken ct = default)
    {
        var apiBaseUri = _options.ApiBaseUri.TrimEnd('/');
        var url = $"{apiBaseUri}/v1/{appId}/messages:send";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Content = JsonContent.Create(message);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
