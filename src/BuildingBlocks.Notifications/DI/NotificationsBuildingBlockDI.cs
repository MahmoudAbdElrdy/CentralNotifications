using BuildingBlocks.Notifications.Contracts;
using BuildingBlocks.Notifications.Core;
using BuildingBlocks.Notifications.Outbox;
using BuildingBlocks.Notifications.Persistence;
using BuildingBlocks.Notifications.Push;
using BuildingBlocks.Notifications.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Notifications.DI;

public sealed class NotificationsModuleOptions
{
    public bool EnableSignalR { get; set; } = true;
    public bool EnableFcm { get; set; } = true;
    public bool EnableHms { get; set; } = true;
    public OutboxWorkerOptions Outbox { get; set; } = new();
}

public static class NotificationsBuildingBlockDI
{
    public static IServiceCollection AddNotificationsBuildingBlockSqlServer(
        this IServiceCollection services,
        string connectionString,
        Action<NotificationsModuleOptions>? configure = null)
    {
        var opt = new NotificationsModuleOptions();
        configure?.Invoke(opt);
        services.AddSingleton(opt);

        services.AddDbContext<NotificationsDbContext>(o => o.UseSqlServer(connectionString));

        services.AddScoped<IUserPushTokenStore, EfUserPushTokenStore>();
        services.AddScoped<IOutboxStore, EfOutboxStore>();

        services.AddScoped<INotificationQueue, OutboxNotificationQueue>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        if (opt.EnableSignalR)
        {
            services.AddSignalR();
            services.AddSingleton<IRealtimeProvider, SignalRRealtimeProvider>();
        }
        else
        {
            services.AddSingleton<IRealtimeProvider, NoopRealtimeProvider>();
        }

        if (opt.EnableFcm)
        {
            services.AddHttpClient<IFcmV1Sender, FcmV1Sender>();
            services.AddSingleton<IPushProvider, FcmPushProvider>();
            services.AddOptions<FcmV1Options>().BindConfiguration("FcmV1");
        }

        if (opt.EnableHms)
        {
            services.AddSingleton<IHmsMessagingFactory, HmsMessagingFactory>();
            services.AddSingleton<IPushProvider, HmsPushProvider>();
            services.AddOptions<HuaweiPushOptions>().BindConfiguration("HuaweiPush");
        }

        services.AddSingleton(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            return new OutboxWorkerOptions
            {
                PollIntervalSeconds = cfg.GetValue("Notifications:Outbox:PollIntervalSeconds", opt.Outbox.PollIntervalSeconds),
                BatchSize = cfg.GetValue("Notifications:Outbox:BatchSize", opt.Outbox.BatchSize),
                MaxRetries = cfg.GetValue("Notifications:Outbox:MaxRetries", opt.Outbox.MaxRetries),
            };
        });

        services.AddHostedService<NotificationOutboxWorker>();

        return services;
    }

    private sealed class NoopRealtimeProvider : IRealtimeProvider
    {
        public Task SendAsync(NotificationMessage msg, CancellationToken ct = default) => Task.CompletedTask;
    }
}
