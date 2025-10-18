using Microsoft.EntityFrameworkCore;                  // ToListAsync vb.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Db;
using Order.Application.Abstractions.Messaging;
using Order.Domain.Outbox;

namespace Order.Publisher.Workers;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _log;
    private readonly IEventPublisher _publisher;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherWorker> log,
        IEventPublisher publisher)
    {
        _scopeFactory = scopeFactory;
        _log = log;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IOrderDbContext>();

                var batch = await db.Outbox
                    .Where(x => x.Status == OutboxStatus.Pending)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                foreach (var msg in batch)
                {
                    try
                    {
                        if (msg.Type == "OrderFulfilledEvent")
                        {
                            await _publisher.PublishRawAsync(
                                exchange: "order.events",
                                routingKey: "order.fulfilled",
                                bodyUtf8: msg.Payload,
                                ct: stoppingToken);

                            msg.Status = OutboxStatus.Processed;
                            msg.ProcessedAtUtc = DateTime.UtcNow;
                            msg.Error = null;
                        }
                        else
                        {
                            msg.Status = OutboxStatus.Failed;
                            msg.Error = $"Unknown type: {msg.Type}";
                        }
                    }
                    catch (Exception ex)
                    {
                        msg.RetryCount += 1;
                        msg.Status = msg.RetryCount >= 10 ? OutboxStatus.Failed : OutboxStatus.Pending;
                        msg.Error = ex.Message;

                        _log.LogWarning(ex,
                            "Outbox publish failed. Id={Id}, Retry={Retry}",
                            msg.Id, msg.RetryCount);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Outbox loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
