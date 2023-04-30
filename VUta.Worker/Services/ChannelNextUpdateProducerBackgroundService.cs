namespace VUta.Worker.Services
{
    using MassTransit;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using VUta.Database;
    using VUta.Transport.Messages;
    using VUta.Worker;

    public class ChannelNextUpdateProducerBackgroundService : BackgroundService
    {
        private readonly ILogger<ChannelNextUpdateProducerBackgroundService> _logger;
        private readonly IBus _bus;
        private readonly IServiceProvider _serviceProvider;

        public ChannelNextUpdateProducerBackgroundService(
            ILogger<ChannelNextUpdateProducerBackgroundService> logger,
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

                if (!opt.Producer.ChannelNextUpdate)
                    break;

                if (firstLoop)
                {
                    firstLoop = false;
                    _logger.LogInformation("Producer {Name} Enabled", nameof(opt.Producer.ChannelNextUpdate));
                }

                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<VUtaDbContext>();
                    using var T = await db.Database.BeginTransactionAsync(stoppingToken);
                    var updateChannels = await db.Channels
                        .Where(x => DateTime.UtcNow > x.NextUpdate && x.NextUpdateId == null)
                        .Take(1000)
                        .For().Update().SkipLocked()
                        .ToDictionaryAsync(k => k.Id, v => v, stoppingToken);

                    if (updateChannels.Any())
                    {
                        foreach (var (id, channel) in updateChannels)
                            channel.NextUpdateId = NewId.NextGuid();

                        await db.SaveChangesAsync(stoppingToken);
                        await T.CommitAsync(stoppingToken);
                        await _bus.PublishBatch(updateChannels.Select(x => new UpdateChannel(x.Key, true)),
                            c => c.CorrelationId = updateChannels[c.Message.Id].NextUpdateId);

                        if (updateChannels.Count >= 1000)
                            continue;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
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
}
