using BuildingBlocks.Notifications.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace NotificationsService.Controllers;

public record RegisterTokenRequest(long UserId, string Token, PushTokenProvider Provider, string? DeviceId);

[ApiController]
[Route("api/push")]
public class PushTokenController : ControllerBase
{
    private readonly IUserPushTokenStore _store;

    public PushTokenController(IUserPushTokenStore store) => _store = store;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTokenRequest req, CancellationToken ct)
    {
        if (req.UserId <= 0) return BadRequest("UserId is required.");
        if (string.IsNullOrWhiteSpace(req.Token)) return BadRequest("Token is required.");

        await _store.UpsertAsync(new UserPushToken
        {
            UserId = req.UserId,
            Token = req.Token,
            Provider = req.Provider,
            DeviceId = req.DeviceId
        }, ct);

        return Ok();
    }
}
