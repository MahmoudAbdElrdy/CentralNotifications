using System.Collections.Concurrent;
using BuildingBlocks.Notifications.Contracts;
using Microsoft.Extensions.Options;

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
    public string AppId { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
}

public sealed class HmsMessagingClient
{
    private readonly IHmsRestClient _restClient;
    private readonly HuaweiAppOptions _appOptions;

    public HmsMessagingClient(IHmsRestClient restClient, HuaweiAppOptions appOptions)
    {
        _restClient = restClient;
        _appOptions = appOptions;
    }

    public async Task SendAsync(HmsMessageRequest message, CancellationToken ct = default)
    {
        var accessToken = await _restClient.GetAccessTokenAsync(_appOptions.ClientId, _appOptions.ClientSecret, ct);
        await _restClient.SendMessageAsync(_appOptions.AppId, accessToken, message, ct);
    }
}

public interface IHmsMessagingFactory
{
    HmsMessagingClient Get(PushTokenProvider provider);
}

public sealed class HmsMessagingFactory : IHmsMessagingFactory
{
    private readonly HuaweiPushOptions _opt;
    private readonly IHmsRestClient _restClient;
    private readonly ConcurrentDictionary<PushTokenProvider, HmsMessagingClient> _cache = new();

    public HmsMessagingFactory(IOptions<HuaweiPushOptions> opt, IHmsRestClient restClient)
    {
        _opt = opt.Value;
        _restClient = restClient;
    }

    public HmsMessagingClient Get(PushTokenProvider provider) => _cache.GetOrAdd(provider, Create);

    private HmsMessagingClient Create(PushTokenProvider provider)
    {
        var key = provider.ToString();
        if (!_opt.Apps.TryGetValue(key, out var appCfg))
            throw new InvalidOperationException($"HuaweiPush:Apps:{key} not configured.");

        return new HmsMessagingClient(_restClient, appCfg);
    }
}
