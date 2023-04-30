namespace VUta.Worker.Consumers
{
    using MassTransit;

    using Microsoft.EntityFrameworkCore;

    using System.Threading.Tasks;

    using VUta.Database;
    using VUta.Transport.Messages;

    using YoutubeExplode;
    using YoutubeExplode.Exceptions;

    public class ScanChannelVideoConsumer
        : IConsumer<ScanChannelVideo>
    {
        private readonly ILogger<ScanChannelVideoConsumer> _logger;
        private readonly VUtaDbContext _db;
        private readonly YoutubeClient _youtube;

        public ScanChannelVideoConsumer(
            ILogger<ScanChannelVideoConsumer> logger,
            VUtaDbContext db,
            YoutubeClient youtube)
        {
            _logger = logger;
            _db = db;
            _youtube = youtube;
        }

        public async Task Consume(ConsumeContext<ScanChannelVideo> context)
        {
            var (channelId, fullScan, _) = context.Message;
            var uploadPlaylistId = "UU" + channelId[2..];
            var addedIds = new List<string>();
            var firstBatch = true;
            try
            {
                await foreach (var batch in _youtube.Playlists.GetVideoBatchesAsync(uploadPlaylistId, context.CancellationToken))
                {
                    firstBatch = false;
                    _logger.LogInformation("Playlist scanned: {ChannelId}, {LastId}", channelId, batch.Items.Last().Id);
                    var videoIds = batch.Items.Select(x => (string)x.Id).ToArray();
                    var existsIds = await _db.Videos
                        .Where(x => videoIds.Contains(x.Id))
                        .Select(x => x.Id)
                        .ToListAsync(context.CancellationToken);

                    foreach (var video in batch.Items.Where(x => !existsIds.Contains(x.Id)))
                    {
                        _db.Videos.Add(new()
                        {
                            Id = video.Id,
                            Title = video.Title,
                            ChannelId = video.Author.ChannelId,
                            PublishDate = DateTime.MinValue,
                        });
                        addedIds.Add(video.Id);
                    }

                    if (!fullScan && existsIds.Any())
                        break;
                }
            }
            catch (PlaylistUnavailableException) when (firstBatch)
            {
                _logger.LogWarning("Playlist {Id} not unavailable", uploadPlaylistId);
            }

            if (addedIds.Any())
            {
                await _db.SaveChangesAsync();
                await context.PublishBatch(addedIds.Select(id => new UpdateVideo(id, true)));
            }
        }
    }
}