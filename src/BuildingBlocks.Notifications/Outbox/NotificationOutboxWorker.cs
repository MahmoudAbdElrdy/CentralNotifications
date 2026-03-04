using BuildingBlocks.Notifications.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Notifications.Outbox;

public sealed class NotificationOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationOutboxWorker> _log;
    private readonly OutboxWorkerOptions _opt;

    public NotificationOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationOutboxWorker> log, OutboxWorkerOptions opt)
    {
        _scopeFactory = scopeFactory;
        _log = log;
        _opt = opt;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

                var now = DateTime.UtcNow;
                var items = await store.DequeueBatchAsync(_opt.BatchSize, now, _opt.MaxRetries, stoppingToken);

                if (items.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_opt.PollIntervalSeconds), stoppingToken);
                    continue;
                }

                await store.MarkProcessingAsync(items.Select(x => x.Id), now, stoppingToken);

                foreach (var it in items)
                {
                    try
                    {
                        var msg = NotificationJson.FromJson(it.PayloadJson);
                        await dispatcher.DispatchAsync(msg, stoppingToken);
                        await store.MarkSentAsync(it.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        var nextRetry = it.RetryCount + 1;
                        var delay = Math.Min((int)Math.Pow(2, nextRetry), 600);
                        var nextAt = DateTime.UtcNow.AddSeconds(delay);

                        await store.MarkFailedAsync(it.Id, nextRetry, nextAt, ex.ToString(), stoppingToken);
                        _log.LogError(ex, "Notification send failed. OutboxId={Id} Retry={Retry}", it.Id, nextRetry);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Outbox worker loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(_opt.PollIntervalSeconds), stoppingToken);
        }
    }
}

public sealed class OutboxWorkerOptions
{
    public int PollIntervalSeconds { get; set; } = 3;
    public int BatchSize { get; set; } = 50;
    public int MaxRetries { get; set; } = 10;
}
