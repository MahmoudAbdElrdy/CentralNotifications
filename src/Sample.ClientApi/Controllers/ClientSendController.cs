using System.Net.Http.Json;
using BuildingBlocks.Notifications.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Sample.ClientApi.Controllers;

[ApiController]
[Route("api/client")]
public class ClientSendController : ControllerBase
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _cfg;

    public ClientSendController(IHttpClientFactory http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    [HttpPost("send/{userId:long}")]
    public async Task<IActionResult> Send(long userId, CancellationToken ct)
    {
        var baseUrl = _cfg.GetValue<string>("NotificationsService:BaseUrl")!.TrimEnd('/');
        var apiKey = _cfg.GetValue<string>("NotificationsService:ApiKey")!;

        var client = _http.CreateClient("notifications");
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("X-Notifications-ApiKey", apiKey);

        var payload = new
        {
            target = userId.ToString(),
            targetType = NotificationTargetType.User,
            title = "From ClientApi",
            body = "This came from another project via HTTP",
            label = "general",
            data = new { any = "value" },
            silent = false,
            timeToLiveSeconds = 60
        };

        var res = await client.PostAsJsonAsync("/api/notifications/send", payload, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, body);
        return Ok();
    }
}
