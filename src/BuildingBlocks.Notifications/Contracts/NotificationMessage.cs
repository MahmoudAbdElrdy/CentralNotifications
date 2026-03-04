namespace BuildingBlocks.Notifications.Contracts;

public sealed class NotificationMessage
{
    public string Target { get; set; } = default!;
    public NotificationTargetType TargetType { get; set; } = NotificationTargetType.User;

    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;

    public string? Label { get; set; }
    public object? Data { get; set; }

    public bool Silent { get; set; } = false;
    public int? TimeToLiveSeconds { get; set; }
}
