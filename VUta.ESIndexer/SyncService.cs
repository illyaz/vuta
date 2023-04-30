namespace VUta.ESIndexer
{
    using Microsoft.EntityFrameworkCore;

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using VUta.Database;

    public class SyncService : BackgroundService
    {
        private readonly ILogger<SyncService> _logger;
        private readonly IHostApplicationLifetime _host;
        private readonly ESIndexerService _indexer;
        private readonly IServiceProvider _serviceProvider;

        public SyncService(
            ILogger<SyncService> logger,
            IHostApplicationLifetime host,
            ESIndexerService indexer,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _host = host;
            _indexer = indexer;
            _serviceProvider = serviceProvider;
            _indexer.WaitShutdown = true;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Syncing index");
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VUtaDbContext>();

            await IndexChannelAsync(db, stoppingToken);
            await IndexCommentAsync(db, stoppingToken);
            await IndexVideoAsync(db, stoppingToken);

            _host.StopApplication();
        }

        private async Task IndexChannelAsync(VUtaDbContext db, CancellationToken stoppingToken)
        {
            var estimateItem = await db.Channels.CountAsync(stoppingToken);
            var i = 0;
            await foreach (var channel in db.Channels
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Thumbnail,
                    x.LastUpdate
                })
                .AsNoTracking()
                .AsAsyncEnumerable()
                .WithCancellation(stoppingToken))
            {
                i++;

                if (i % _indexer.BatchSize == 0)
                    _logger.LogInformation("Reading channel [{i}/{est}]", i, estimateItem);

                await _indexer.AddDataAsync(new("channels", new()
                {
                    { "id", channel.Id },
                    { "title", channel.Title },
                    { "thumbnail", channel.Thumbnail },
                    { "last_update", channel.LastUpdate }
                }, null, false));
            }
        }

        private async Task IndexVideoAsync(VUtaDbContext db, CancellationToken stoppingToken)
        {
            var estimateItem = await db.Videos.CountAsync(stoppingToken);
            var i = 0;
            await foreach (var video in db.Videos
                .Select(x => new
                {
                    x.Id,
                    x.ChannelId,
                    x.Title,
                    x.PublishDate,
                    x.LastUpdate,
                    x.IsUta
                })
                .AsNoTracking()
                .AsAsyncEnumerable()
                .WithCancellation(stoppingToken))
            {
                i++;

                if (i % _indexer.BatchSize == 0)
                    _logger.LogInformation("Reading video [{i}/{est}]", i, estimateItem);

                await _indexer.AddDataAsync(new("videos", new()
                {
                    { "id", video.Id },
                    { "channel_id", video.ChannelId },
                    { "title", video.Title },
                    { "publish_date", video.PublishDate },
                    { "last_update", video.LastUpdate },
                    { "is_uta", video.IsUta }
                }, null, false));
            }
        }

        private async Task IndexCommentAsync(VUtaDbContext db, CancellationToken stoppingToken)
        {
            var estimateItem = await db.Comments.CountAsync(stoppingToken);
            var i = 0;
            await foreach (var comment in db.Comments
                .Select(x => new
                {
                    x.Id,
                    x.VideoId,
                    x.Video.ChannelId,
                    x.Text,
                    x.LikeCount,
                    x.LastUpdate,
                    x.Video.PublishDate,
                    x.Video.IsUta
                })
                .AsNoTracking()
                .AsAsyncEnumerable()
                .WithCancellation(stoppingToken))
            {
                i++;

                if (i % _indexer.BatchSize == 0)
                    _logger.LogInformation("Reading comment [{i}/{est}]", i, estimateItem);

                await _indexer.AddDataAsync(new("comments", new()
                {
                    { "id", comment.Id },
                    { "video_id", comment.VideoId },
                    { "video_publish_date", comment.PublishDate },
                    { "video_is_uta", comment.IsUta },
                    { "channel_id", comment.ChannelId },
                    { "text", comment.Text },
                    { "like_count", comment.LikeCount },
                    { "last_update", comment.LastUpdate }
                }, null, false));
            }
        }
    }
}
