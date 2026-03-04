using System.Text.Json;

namespace BuildingBlocks.Notifications.Contracts;

public static class NotificationJson
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string ToJson(NotificationMessage msg) => JsonSerializer.Serialize(msg, Opt);

    public static NotificationMessage FromJson(string json) =>
        JsonSerializer.Deserialize<NotificationMessage>(json, Opt)
        ?? throw new InvalidOperationException("Invalid notification json payload.");
}
