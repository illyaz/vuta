namespace VUta.Worker.Consumers
{
    using MassTransit;

    using Microsoft.EntityFrameworkCore;

    using System.Threading.Tasks;

    using VUta.Database;
    using VUta.Transport.Messages;

    using YoutubeExplode;
    using YoutubeExplode.Exceptions;

    public class AddChannelConsumer
        : IConsumer<AddChannel>
    {
        private readonly ILogger<AddChannelConsumer> _logger;
        private readonly VUtaDbContext _db;
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
                var channel = await _youtube.Channels
                    .GetInnertubeAsync(id);

                var result = await _db.Channels
                    .Upsert(new()
                    {
                        Id = channel.Id,
                        Title = channel.Title,
                        Thumbnail = channel.Thumbnails.First().Url
                    })
                    .AllowIdentityMatch()
                    .NoUpdate()
                    .RunAsync(context.CancellationToken);

                var added = result != 0;

                // Perform full channel scan
                if (added)
                    await context.Publish<ScanChannelVideo>(new(id, true), context.CancellationToken);

                if (context.IsResponseAccepted<AddChannelResult>())
                    await context.RespondAsync<AddChannelResult>(new(
                        !added, new(
                            channel.Title,
                            channel.Thumbnails.First().Url)));
            }
            catch (ChannelUnavailableException ex)
            {
                _logger.LogWarning(ex, "An error occurred while adding channel");
                if (context.IsResponseAccepted<AddChannelResult>())
                    await context.RespondAsync<AddChannelResult>(new(false, null, ex.Message));
            }
        }
    }
}