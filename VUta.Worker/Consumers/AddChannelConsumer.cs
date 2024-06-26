﻿using MassTransit;
using Microsoft.EntityFrameworkCore;
using VUta.Database;
using VUta.Transport.Messages;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Exceptions;

namespace VUta.Worker.Consumers;

public class AddChannelConsumer
    : IConsumer<AddChannel>
{
    private readonly VUtaDbContext _db;
    private readonly ILogger<AddChannelConsumer> _logger;
    private readonly YoutubeClient _youtube;

    public AddChannelConsumer(
        ILogger<AddChannelConsumer> logger,
        VUtaDbContext db,
        YoutubeClient youtube)
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

            var channel = null as Channel;
            if (ChannelId.TryParse(id) is ChannelId channelId)
            {
                channel = await _youtube.Channels
                    .GetInnertubeAsync(channelId);
            }
            else if (id.StartsWith("@"))
            {
                channel = await _youtube.Channels
                    .GetByHandleAsync(id[1..]);
            }
            else if (ChannelHandle.TryParse(id) is ChannelHandle handle)
            {
                channel = await _youtube.Channels
                    .GetByHandleAsync(handle);
            }
            else if (ChannelSlug.TryParse(id) is ChannelSlug slug)
            {
                channel = await _youtube.Channels
                    .GetBySlugAsync(slug);
            }
            else
            {
                _logger.LogWarning("Invalid channel identifier");
                if (context.IsResponseAccepted<AddChannelResult>())
                    await context.RespondAsync(new AddChannelResult(false, null, "Invalid channel identifier"));

                return;
            }

            var result = await _db.Channels
                .Upsert(new Database.Models.Channel
                {
                    Id = channel.Id,
                    Title = channel.Title,
                    Description = "",
                    Thumbnail = channel.Thumbnails.First().Url,
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
                await context.RespondAsync<AddChannelResult>(new AddChannelResult(
                    !added, new AddChannelResultInfo(
                        channel.Title,
                        channel.Thumbnails.First().Url)));
        }
        catch (ChannelUnavailableException ex)
        {
            _logger.LogWarning(ex, "An error occurred while adding channel");
            if (context.IsResponseAccepted<AddChannelResult>())
                await context.RespondAsync(new AddChannelResult(false, null, ex.Message));
        }
    }
}