using BuildingBlocks.Notifications.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace NotificationsService.Controllers;

public sealed record SendNotificationRequest(
    string Target,
    NotificationTargetType TargetType,
    string Title,
    string Body,
    string? Label,
    object? Data,
    bool Silent,
    int? TimeToLiveSeconds
);

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly INotificationQueue _queue;

    public NotificationsController(IConfiguration cfg, INotificationQueue queue)
    {
        _cfg = cfg;
        _queue = queue;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest req, CancellationToken ct)
    {
        // Simple API key check (replace with JWT if needed)
        var apiKey = _cfg.GetValue<string>("NotificationsService:ApiKey");
        var header = Request.Headers["X-Notifications-ApiKey"].ToString();

        if (string.IsNullOrWhiteSpace(apiKey) || header != apiKey)
            return Unauthorized("Invalid API key.");

        await _queue.EnqueueAsync(new NotificationMessage
        {
            Target = req.Target,
            TargetType = req.TargetType,
            Title = req.Title,
            Body = req.Body,
            Label = req.Label,
            Data = req.Data,
            Silent = req.Silent,
            TimeToLiveSeconds = req.TimeToLiveSeconds
        }, ct);

        return Ok();
    }
}
