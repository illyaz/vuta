using System.Net;
using Google;
using Google.Apis.YouTube.v3;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VUta.Database;
using VUta.Database.Models;
using VUta.Transport.Messages;
using YoutubeExplode;
using YoutubeExplode.Exceptions;

namespace VUta.Worker.Consumers;

public class ScanChannelVideoConsumer
    : IConsumer<ScanChannelVideo>
{
    private readonly VUtaDbContext _db;
    private readonly ILogger<ScanChannelVideoConsumer> _logger;
    private readonly YouTubeService _youtube;

    public ScanChannelVideoConsumer(
        ILogger<ScanChannelVideoConsumer> logger,
        VUtaDbContext db,
        YouTubeService youtube)
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
            var listRequest = _youtube.PlaylistItems.List("snippet");
            listRequest.PlaylistId = uploadPlaylistId;
            listRequest.MaxResults = 50;
            listRequest.Fields = "nextPageToken,items.snippet(title,resourceId.videoId,videoOwnerChannelId)";

            while (true)
            {
                var listResponse = await listRequest.ExecuteAsync(context.CancellationToken);
                firstBatch = false;

                _logger.LogInformation("Playlist scanned: {ChannelId}, {LastId}", uploadPlaylistId,
                    listResponse.Items.Last().Snippet.ResourceId.VideoId);

                var videoIds = listResponse.Items.Select(x => x.Snippet.ResourceId.VideoId).ToArray();
                var existsIds = await _db.Videos
                    .Where(x => videoIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync(context.CancellationToken);

                foreach (var video in listResponse.Items.Where(x => !existsIds.Contains(x.Snippet.ResourceId.VideoId)))
                {
                    _db.Videos.Add(new Video
                    {
                        Id = video.Snippet.ResourceId.VideoId,
                        Title = video.Snippet.Title,
                        ChannelId = video.Snippet.VideoOwnerChannelId,
                        PublishDate = DateTime.MinValue
                    });
                    addedIds.Add(video.Snippet.ResourceId.VideoId);
                }

                if (!fullScan && existsIds.Any())
                    break;

                if (string.IsNullOrEmpty(listResponse.NextPageToken))
                    break;

                listRequest.PageToken = listResponse.NextPageToken;
            }
        }
        catch (GoogleApiException e) when (firstBatch && e.HttpStatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Playlist {Id} not unavailable", uploadPlaylistId);
        }

        if (addedIds.Any())
        {
            try
            {
                await _db.SaveChangesAsync(context.CancellationToken);
            }
            catch (DbUpdateException dbe)
                when (dbe.InnerException is PostgresException
                      {
                          SqlState: PostgresErrorCodes.ForeignKeyViolation,
                          ConstraintName: "fk_videos_channels_channel_id"
                      })
            {
                if (dbe.Entries
                        .Select(x => x.Entity)
                        .OfType<Video>().Select(x => x.ChannelId)
                        .Distinct()
                        .FirstOrDefault(x => x != channelId) is
                    { } anotherChannelId)
                {
                    _db.Channels.Add(new Channel
                    {
                        Id = anotherChannelId,
                        Title = "Unknown",
                        Description = string.Empty,
                        Thumbnail = string.Empty,
                        NextUpdate = DateTime.UtcNow
                    });

                    await _db.SaveChangesAsync(context.CancellationToken);
                }
                else
                    throw;
            }

            await context.PublishBatch(addedIds.Select(id => new UpdateVideo(id, true)));
        }
    }
}