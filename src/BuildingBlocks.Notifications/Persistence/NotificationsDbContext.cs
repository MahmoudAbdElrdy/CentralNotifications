using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Notifications.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuildingBlocks.Notifications.Persistence;

public sealed class NotificationsDbContext : DbContext
{
    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

    public DbSet<UserPushTokenEntity> UserPushTokens => Set<UserPushTokenEntity>();
    public DbSet<OutboxEntity> Outbox => Set<OutboxEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var providerConverter = new EnumToNumberConverter<PushTokenProvider, int>();
        var statusConverter = new EnumToNumberConverter<OutboxStatus, int>();

        b.Entity<UserPushTokenEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasConversion(providerConverter);
            e.Property(x => x.Token).HasMaxLength(1024);
            e.HasIndex(x => new { x.UserId, x.Provider, x.DeviceId });
            e.HasIndex(x => new { x.UserId, x.Token });
        });

        b.Entity<OutboxEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion(statusConverter);
            e.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
            e.Property(x => x.LastError).HasColumnType("nvarchar(max)");
            e.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
        });
    }
}

public sealed class UserPushTokenEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string Token { get; set; } = default!;
    public PushTokenProvider Provider { get; set; }
    public string? DeviceId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class OutboxEntity
{
    public long Id { get; set; }
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    public string PayloadJson { get; set; } = default!;
    public int RetryCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;

    public string? LastError { get; set; }
}
