using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VUta.Database;
using VUta.Database.Models;
using VUta.Transport.Messages;

namespace VUta.Worker.Services;

public class ChannelNextUpdateStuckProducerBackgroundService : BackgroundService
{
    private readonly IBus _bus;
    private readonly ILogger<ChannelNextUpdateStuckProducerBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ChannelNextUpdateStuckProducerBackgroundService(
        ILogger<ChannelNextUpdateStuckProducerBackgroundService> logger,
        IBus bus,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _bus = bus;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstLoop = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var opt = scope.ServiceProvider.GetRequiredService<IOptions<WorkerOptions>>().Value;

            if (!opt.Producer.ChannelNextUpdateStuck)
                break;

            if (firstLoop)
            {
                firstLoop = false;
                _logger.LogInformation("Producer {Name} Enabled", nameof(opt.Producer.ChannelNextUpdateStuck));
            }

            try
            {
                var db = scope.ServiceProvider.GetRequiredService<VUtaDbContext>();
                using var T = await db.Database.BeginTransactionAsync(stoppingToken);

                var updateChannels = await db.Channels
                    .Where(sc => sc.NextUpdate != null && DateTime.UtcNow - TimeSpan.FromHours(1) > sc.NextUpdate)
                    .Take(1000)
                    .For().Update().SkipLocked()
                    .ToDictionaryAsync(k => k.Id, v => v, stoppingToken);

                if (updateChannels.Any())
                {
                    _logger.LogWarning("Requeue {Count} stucked {Type} update", updateChannels.Count, nameof(Channel));

                    var date = DateTime.UtcNow;
                    foreach (var (id, channel) in updateChannels)
                    {
                        channel.NextUpdateId = NewId.NextGuid();
                        channel.NextUpdate = date;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    await T.CommitAsync(stoppingToken);
                    await _bus.PublishBatch(updateChannels.Select(x => new UpdateChannel(x.Key, true)),
                        c => c.CorrelationId = updateChannels[c.Message.Id].NextUpdateId,
                        cancellationToken: stoppingToken);

                    if (updateChannels.Count >= 1000)
                        continue;
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
                when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "An error occurred while executing");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}