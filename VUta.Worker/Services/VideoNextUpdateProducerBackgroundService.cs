namespace VUta.Worker.Services
{
    using MassTransit;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using VUta.Database;
    using VUta.Transport.Messages;
    using VUta.Worker;

    public class VideoNextUpdateProducerBackgroundService : BackgroundService
    {
        private readonly ILogger<VideoNextUpdateProducerBackgroundService> _logger;
        private readonly IBus _bus;
        private readonly IServiceProvider _serviceProvider;

        public VideoNextUpdateProducerBackgroundService(
            ILogger<VideoNextUpdateProducerBackgroundService> logger,
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

                if (!opt.Producer.VideoNextUpdate)
                    break;

                if (firstLoop)
                {
                    firstLoop = false;
                    _logger.LogInformation("Producer {Name} Enabled", nameof(opt.Producer.VideoNextUpdate));
                }

                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<VUtaDbContext>();
                    using var T = await db.Database.BeginTransactionAsync(stoppingToken);
                    var updateVideos = await db.Videos
                        .Where(x => DateTime.UtcNow > x.NextUpdate && x.NextUpdateId == null)
                        .Take(1000)
                        .For().Update().SkipLocked()
                        .ToDictionaryAsync(k => k.Id, v => v, stoppingToken);

                    if (updateVideos.Any())
                    {
                        foreach (var (id, video) in updateVideos)
                            video.NextUpdateId = NewId.NextGuid();

                        await db.SaveChangesAsync(stoppingToken);
                        await T.CommitAsync(stoppingToken);
                        await _bus.PublishBatch(updateVideos.Select(x => new UpdateVideo(x.Key, true)),
                            c => c.CorrelationId = updateVideos[c.Message.Id].NextUpdateId);

                        if (updateVideos.Count >= 1000)
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
