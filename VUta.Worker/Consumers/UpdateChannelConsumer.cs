﻿namespace VUta.Worker.Consumers
{
    using MassTransit;

    using Microsoft.EntityFrameworkCore;

    using System.Threading.Tasks;

    using VUta.Database;
    using VUta.Transport.Messages;

    using YoutubeExplode;
    using YoutubeExplode.Exceptions;

    public class UpdateChannelConsumer
        : IConsumer<UpdateChannel>
    {
        private readonly ILogger<UpdateChannelConsumer> _logger;
        private readonly VUtaDbContext _db;
        private readonly YoutubeClient _youtube;

        public UpdateChannelConsumer(
            ILogger<UpdateChannelConsumer> logger,
            VUtaDbContext db,
            YoutubeClient youtube)
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
                _logger.LogWarning("Update id not matched", id);
                return;
            }

            if (channel != null)
            {
                try
                {
                    var channelMeta = await _youtube.Channels
                        .GetInnertubeAsync(id, context.CancellationToken);

                    channel.Title = channelMeta.Title;
                    channel.Thumbnail = channelMeta.Thumbnails[0].Url;
                    channel.Handle = channelMeta.Handle;

                    if (lastVideoPublish > DateTime.UtcNow.AddDays(-2))
                        channel.NextUpdate = DateTime.UtcNow.AddHours(3);
                    else if (lastVideoPublish > DateTime.UtcNow.AddDays(-3))
                        channel.NextUpdate = DateTime.UtcNow.AddHours(6);
                    else if (lastVideoPublish > DateTime.UtcNow.AddDays(-7))
                        channel.NextUpdate = DateTime.UtcNow.AddDays(1);
                    else if (lastVideoPublish > DateTime.UtcNow.AddDays(-14))
                        channel.NextUpdate = DateTime.UtcNow.AddDays(3);
                    else if (lastVideoPublish > DateTime.UtcNow.AddDays(-30))
                        channel.NextUpdate = DateTime.UtcNow.AddDays(6);
                    else if (lastVideoPublish > DateTime.UtcNow.AddDays(-180))
                        channel.NextUpdate = DateTime.UtcNow.AddDays(30);
                    else
                        channel.NextUpdate = DateTime.UtcNow.AddDays(1);

                    exists = true;
                }
                catch (ChannelUnavailableException)
                {
                    channel.UnavailableSince ??= DateTime.UtcNow;
                    if (channel.UnavailableSince >  DateTime.UtcNow.AddDays(-30))
                        channel.NextUpdate = DateTime.UtcNow.AddDays(1);
                    else
                        channel.NextUpdate = null;
                }

                channel.LastUpdate = DateTime.UtcNow;
                channel.NextUpdateId = null;

                await _db.SaveChangesAsync(context.CancellationToken);

                if (exists && scanVideo)
                    await context.Publish<ScanChannelVideo>(new(id), context.CancellationToken);
            }
            else
                _logger.LogWarning("Channel {Id} not exists in database", id);
        }
    }
}