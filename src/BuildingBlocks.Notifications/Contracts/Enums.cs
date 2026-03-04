namespace BuildingBlocks.Notifications.Contracts;

public enum NotificationTargetType { User, Group, Topic }

public enum PushTokenProvider
{
    WebFcm,
    AndroidFcm,
    AppleFcm,
    HuaweiUser,
    HuaweiDriver,
    HuaweiMerchant
}

public enum OutboxStatus { Pending, Processing, Sent, Failed }
