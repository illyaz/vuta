using System.Text.RegularExpressions;
using Google;
using Google.Apis.YouTube.v3;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VUta.Database;
using VUta.Transport.Messages;
using Video = VUta.Database.Models.Video;

namespace VUta.Worker.Consumers;

public partial class ScanVideoCommentConsumer :
    IConsumer<ScanVideoComment>
{
    private readonly VUtaDbContext _db;
    private readonly ILogger<ScanVideoCommentConsumer> _logger;
    private readonly YouTubeService _youtube;

    public ScanVideoCommentConsumer(
        ILogger<ScanVideoCommentConsumer> logger,
        VUtaDbContext db,
        YouTubeService youtube)
    {
        _logger = logger;
        _db = db;
        _youtube = youtube;
    }

    public async Task Consume(ConsumeContext<ScanVideoComment> context)
    {
        var id = context.Message.Id;
        try
        {
            var listRequest = _youtube.CommentThreads.List(new[] { "snippet", "replies" });
            listRequest.VideoId = id;
            listRequest.Order = CommentThreadsResource.ListRequest.OrderEnum.Relevance;
            listRequest.MaxResults = 100;
            listRequest.Fields =
                "items(snippet.topLevelComment(id,snippet(parentId,textOriginal,likeCount)),replies.comments(id,snippet(parentId,textOriginal,likeCount)))";

            var listResponse = await listRequest.ExecuteAsync();
            var timestampComments = listResponse.Items
                .Select(x => x.Snippet.TopLevelComment)
                .Concat(listResponse.Items.SelectMany(x => x.Replies?.Comments ?? []))
                .Where(x => TimestampRegex()
                    .Matches(x.Snippet.TextOriginal)
                    .Where(m => m.Success && TryParseTimestamp(m.Value, out _))
                    .Take(2)
                    .Count() == 2)
                .ToList();

            if (timestampComments.Any())
                await _db.Comments
                    .UpsertRange(timestampComments
                        .Select(comment => new Database.Models.Comment
                        {
                            Id = comment.Id,
                            VideoId = id,
                            LikeCount = comment.Snippet.LikeCount ?? 0,
                            LastUpdate = DateTime.UtcNow,
                            RepliesId = comment.Snippet.ParentId,
                            Text = comment.Snippet.TextOriginal.Replace("\0", string.Empty)
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

            var video = _db.Attach(new Video { Id = id }).Entity;
            video.LastCommentScan = DateTime.UtcNow;
            await _db.SaveChangesAsync(context.CancellationToken);
        }
        catch (GoogleApiException gae)
        {
            var errors = gae.Error.Errors.Select(x => x.Reason)
                .Where(x => x != "commentsDisabled" && x != "forbidden" && x != "videoNotFound")
                .ToArray();

            if (errors.Any())
            {
                _logger.LogDebug("Video {VideoId} comment error: {Reasons}", id,
                    gae.Error.Errors.Select(x => x.Reason));
                throw;
            }
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

    [GeneratedRegex("[0-9]{1,2}(\\:[0-9]{1,2})?\\:[0-9]{2}")]
    private static partial Regex TimestampRegex();
}