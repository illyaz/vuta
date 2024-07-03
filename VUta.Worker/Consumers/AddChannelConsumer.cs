using Google.Apis.YouTube.v3;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VUta.Database;
using VUta.Transport.Messages;
using YoutubeExplode.Channels;
using YoutubeExplode.Exceptions;

namespace VUta.Worker.Consumers;

public class AddChannelConsumer
    : IConsumer<AddChannel>
{
    private readonly VUtaDbContext _db;
    private readonly ILogger<AddChannelConsumer> _logger;
    private readonly YouTubeService _youtube;

    public AddChannelConsumer(
        ILogger<AddChannelConsumer> logger,
        VUtaDbContext db,
        YouTubeService youtube)
    {
        _logger = logger;
        _db = db;
        _youtube = youtube;
    }

    public async Task Consume(ConsumeContext<AddChannel> context)
    {
        try
        {
            var id = context.Message.Id;
            var listRequest = _youtube.Channels.List("snippet");

            if (ChannelId.TryParse(id) is { } channelId)
                listRequest.Id = channelId.Value;
            else if (id.StartsWith("@"))
                listRequest.ForHandle = id;
            else if (ChannelHandle.TryParse(id) is { } handle)
                listRequest.ForHandle = handle;
            else if (ChannelSlug.TryParse(id) is { } slug)
                listRequest.ForUsername = slug.Value;
            else
            {
                _logger.LogWarning("Invalid channel identifier");
                if (context.IsResponseAccepted<AddChannelResult>())
                    await context.RespondAsync(new AddChannelResult(false, null, "Invalid channel identifier"));

                return;
            }

            var listResponse = await listRequest.ExecuteAsync(context.CancellationToken);
            if (listResponse.Items.FirstOrDefault() is { } channel)
            {
                var result = await _db.Channels
                    .Upsert(new Database.Models.Channel
                    {
                        Id = channel.Id,
                        Title = channel.Snippet.Title,
                        Description = channel.Snippet.Description,
                        Thumbnail = channel.Snippet.Thumbnails.Default__.Url,
                        NextUpdate = DateTime.UtcNow
                    })
                    .AllowIdentityMatch()
                    .WhenMatched(v => new Database.Models.Channel
                    {
                        NextUpdate = DateTime.UtcNow
                    })
                    .RunAsync(context.CancellationToken);

                var added = result != 0;

                // Perform full channel scan
                if (added)
                    await context.Publish(new ScanChannelVideo(channel.Id, true), context.CancellationToken);

                if (context.IsResponseAccepted<AddChannelResult>())
                    await context.RespondAsync(new AddChannelResult(
                        !added, new AddChannelResultInfo(
                            channel.Snippet.Title,
                            channel.Snippet.Thumbnails.Default__.Url)));
            }
        }
        catch (ChannelUnavailableException ex)
        {
            _logger.LogWarning(ex, "An error occurred while adding channel");
            if (context.IsResponseAccepted<AddChannelResult>())
                await context.RespondAsync(new AddChannelResult(false, null, ex.Message));
        }
    }
}