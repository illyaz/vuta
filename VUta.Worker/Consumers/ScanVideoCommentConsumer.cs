using MassTransit;
using Microsoft.EntityFrameworkCore;
using VUta.Database;
using VUta.Transport.Messages;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace VUta.Worker.Consumers;

public class ScanVideoCommentConsumer :
    IConsumer<ScanVideoComment>
{
    private readonly VUtaDbContext _db;
    private readonly ILogger<ScanVideoCommentConsumer> _logger;
    private readonly YoutubeClient _youtube;

    public ScanVideoCommentConsumer(
        ILogger<ScanVideoCommentConsumer> logger,
        VUtaDbContext db,
        YoutubeClient youtube)
    {
        _logger = logger;
        _db = db;
        _youtube = youtube;
    }

    public async Task Consume(ConsumeContext<ScanVideoComment> context)
    {
        var id = context.Message.Id;
        var continuation = await _youtube.Videos.GetCommentTokenAsync(id, context.CancellationToken);
        if (continuation != null)
        {
            var timestampComments = new List<Comment>();

            // Limit 2 batches approximately 35 - 40 comments
            var count = 0;
            while (continuation != null && count++ < 2)
            {
                var batch = await _youtube.Videos
                    .GetCommentBatchAsync(continuation, context.CancellationToken);

                timestampComments.AddRange(batch.Comments
                    .Where(x => x.Runs.Any(r => TryParseTimestamp(r, out _))
                                && !timestampComments.Any(y => y.Id == x.Id)
                                // Filtered timestamp only comment
                                && !x.Runs
                                    .Where(x => x.Trim() != string.Empty)
                                    .All(r => TryParseTimestamp(r, out _))
                                && x.Runs
                                    .Where(x => x.Trim() != string.Empty)
                                    .Count(r => TryParseTimestamp(r, out _)) > 2));

                continuation = batch.Continuation;
            }

            if (timestampComments.Any())
                await _db.Comments
                    .UpsertRange(timestampComments
                        .Select(comment => new Database.Models.Comment
                        {
                            Id = comment.Id,
                            VideoId = id,
                            LikeCount = comment.LikeCount,
                            RepliesId = comment.RepliesId,
                            LastUpdate = DateTime.UtcNow,
                            Text = string.Join("", comment.Runs)
                        }))
                    .AllowIdentityMatch()
                    .WhenMatched((ds, dsi) => new Database.Models.Comment
                    {
                        Text = dsi.Text,
                        RepliesId = dsi.RepliesId,
                        LikeCount = dsi.LikeCount,
                        LastUpdate = dsi.LastUpdate
                    })
                    .RunAsync(context.CancellationToken);
        }
        else
        {
            _logger.LogDebug("Video comment {Id} not exists", id);
        }

        var video = await _db.Videos.FindAsync(id, context.CancellationToken);
        if (video != null)
        {
            video.LastCommentScan = DateTime.UtcNow;
            await _db.SaveChangesAsync(context.CancellationToken);
        }
    }

    private bool TryParseTimestamp(string text, out TimeSpan result)
    {
        return TimeSpan.TryParseExact(text, new[]
        {
            @"m\:ss",
            @"h\:m\:ss"
        }, null, out result);
    }
}