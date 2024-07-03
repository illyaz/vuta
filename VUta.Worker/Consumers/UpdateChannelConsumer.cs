using Google.Apis.YouTube.v3;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VUta.Database;
using VUta.Transport.Messages;

namespace VUta.Worker.Consumers;

public class UpdateChannelConsumer
    : IConsumer<UpdateChannel>
{
    private readonly VUtaDbContext _db;
    private readonly ILogger<UpdateChannelConsumer> _logger;
    private readonly YouTubeService _youtube;

    public UpdateChannelConsumer(
        ILogger<UpdateChannelConsumer> logger,
        VUtaDbContext db,
        YouTubeService youtube)
    {
        _logger = logger;
        _db = db;
        _youtube = youtube;
    }

    public async Task Consume(ConsumeContext<UpdateChannel> context)
    {
        var (id, scanVideo) = context.Message;
        var result = await _db.Channels
            .Where(x => x.Id == id)
            .Select(channel => new
            {
                Channel = channel,
                LastVideoPublishDate = channel.Videos
                    .OrderByDescending(v => v.PublishDate)
                    .Select(v => v.PublishDate)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(context.CancellationToken);


        var exists = false;
        var channel = result?.Channel;
        var lastVideoPublish = result?.LastVideoPublishDate;
        if (channel?.NextUpdateId != context.CorrelationId)
        {
            _logger.LogWarning("Update id not matched: {Id}", context.CorrelationId);
            return;
        }

        if (channel != null)
        {
            var listRequest = _youtube.Channels.List(new[] { "snippet", "statistics", "brandingSettings" });
            listRequest.Id = id;
            var listResponse = await listRequest.ExecuteAsync(context.CancellationToken);

            if (listResponse.Items?.FirstOrDefault() is { } channelResponse)
            {
                channel.Title = channelResponse.Snippet.Title;
                channel.Description = channelResponse.Snippet.Description;
                channel.VideoCount = (long)(channelResponse.Statistics.VideoCount ?? 0);
                channel.SubscriberCount = (long?)channelResponse.Statistics.SubscriberCount;
                channel.Thumbnail = channelResponse.Snippet.Thumbnails.Default__.Url;
                channel.Banner = channelResponse.BrandingSettings.Image?.BannerExternalUrl;
                channel.Handle = string.IsNullOrEmpty(channelResponse.Snippet.CustomUrl)
                    ? null
                    : channelResponse.Snippet.CustomUrl.StartsWith("@")
                        ? channelResponse.Snippet.CustomUrl[1..]
                        : channelResponse.Snippet.CustomUrl;

                if (lastVideoPublish > DateTime.UtcNow.AddDays(-2))
                    channel.NextUpdate = DateTime.UtcNow.AddHours(1);
                else if (lastVideoPublish > DateTime.UtcNow.AddDays(-3))
                    channel.NextUpdate = DateTime.UtcNow.AddHours(3);
                else if (lastVideoPublish > DateTime.UtcNow.AddDays(-7))
                    channel.NextUpdate = DateTime.UtcNow.AddDays(6);
                else if (lastVideoPublish > DateTime.UtcNow.AddDays(-14))
                    channel.NextUpdate = DateTime.UtcNow.AddDays(1);
                else
                    channel.NextUpdate = DateTime.UtcNow.AddDays(1);

                channel.UnavailableSince = null;
                exists = true;
            }
            else
            {
                channel.UnavailableSince ??= DateTime.UtcNow;
                if (channel.UnavailableSince > DateTime.UtcNow.AddDays(-30))
                    channel.NextUpdate = DateTime.UtcNow.AddDays(1);
                else
                    channel.NextUpdate = null;
            }

            channel.LastUpdate = DateTime.UtcNow;
            channel.NextUpdateId = null;

            await _db.SaveChangesAsync(context.CancellationToken);

            if (exists && scanVideo)
                await context.Publish<ScanChannelVideo>(new ScanChannelVideo(id), context.CancellationToken);
        }
        else
        {
            _logger.LogWarning("Channel {Id} not exists in database", id);
        }
    }
}