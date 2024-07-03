using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Google.Apis.YouTube.v3;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using VUta.Database;
using VUta.Transport.Messages;

namespace VUta.Worker.Consumers;

public partial class UpdateVideoConsumer
    : IConsumer<Batch<UpdateVideo>>
{
    private static readonly string[] _utaList =
    {
        "歌",
        "曲",
        "公式mv",
        "originalmv",
        "オリジナルmv",
        "アニソン",
        "うた",
        "カバー",
        "アコギ",
        "officialvideo",
        "불러보",
        "커버",
        "MV",
        "SING",
        "SONG",
        "VOCALOID",
        "ร้อง",
        "เพลง",
        "คาราโอเกะ",
        "anisong",
        "sing",
        "singing",
        "sang",
        "song",
        "karaoke",
        "music",
        "cover",
        "covered",
        "piano",
        "guitar"
    };

    private readonly VUtaDbContext _db;
    private readonly ILogger<UpdateVideoConsumer> _logger;
    private readonly YouTubeService _youtube;

    public UpdateVideoConsumer(
        ILogger<UpdateVideoConsumer> logger,
        VUtaDbContext db,
        YouTubeService youtube)
    {
        _logger = logger;
        _db = db;
        _youtube = youtube;
    }
    
    public async Task Consume(ConsumeContext<Batch<UpdateVideo>> context)
    {
        var messages = context.Message
            .ToImmutableDictionary(
                k => k.Message.Id,
                v => v.Message);

        var dbVideos = await _db.Videos
            .Where(x => messages.Keys.Contains(x.Id))
            .ToDictionaryAsync(
                k => k.Id,
                v => v, context.CancellationToken);

        var listRequest = _youtube.Videos.List(new[] { "snippet", "statistics" });
        listRequest.Id = messages.Keys.ToArray();
        var listResponse = (await listRequest.ExecuteAsync(context.CancellationToken))
            .Items
            .ToDictionary(k => k.Id, v => v);

        var scanCommentMessages = new List<ScanVideoComment>();
        foreach (var (_, message) in messages)
        {
            var (id, scanComment, correlationId) = message;
            var video = dbVideos.GetValueOrDefault(id);
            var exists = false;

            if (correlationId != null && video?.NextUpdateId != correlationId)
            {
                _logger.LogWarning("Update id not matched: {Id}", id);
                continue;
            }

            if (video != null)
            {
                if (listResponse.GetValueOrDefault(id) is { } videoResponse)
                {
                    var replacedTitle = NonWorldRegex().Replace(videoResponse.Snippet.Title, string.Empty);
                    video.Title = videoResponse.Snippet.Title;
                    video.IsUta = _utaList.Any(replacedTitle.Contains);
                    video.ViewCount = (long)(videoResponse.Statistics.ViewCount ?? 0);
                    video.PublishDate = videoResponse.Snippet.PublishedAtDateTimeOffset?.UtcDateTime ?? default;

                    if (video.PublishDate > DateTime.UtcNow.AddDays(-1))
                        video.NextUpdate = DateTime.UtcNow.AddMinutes(15);
                    else if (video.PublishDate > DateTime.UtcNow.AddDays(-3))
                        video.NextUpdate = DateTime.UtcNow.AddMinutes(30);
                    else if (video.PublishDate > DateTime.UtcNow.AddDays(-4))
                        video.NextUpdate = DateTime.UtcNow.AddHours(3);
                    else if (video.PublishDate > DateTime.UtcNow.AddDays(-5))
                        video.NextUpdate = DateTime.UtcNow.AddHours(6);
                    else if (video.PublishDate > DateTime.UtcNow.AddDays(-7))
                        video.NextUpdate = DateTime.UtcNow.AddDays(12);
                    else if (video.PublishDate > DateTime.UtcNow.AddDays(-14))
                        video.NextUpdate = DateTime.UtcNow.AddDays(3);
                    else if (video.PublishDate > DateTime.UtcNow.AddDays(-30))
                        video.NextUpdate = DateTime.UtcNow.AddDays(7);
                    else if (video.PublishDate > DateTime.UtcNow.AddDays(-90))
                        video.NextUpdate = DateTime.UtcNow.AddDays(14);
                    else
                        video.NextUpdate = null;

                    exists = true;
                }
                else
                {
                    video.UnavailableSince ??= DateTime.UtcNow;
                    if (video.UnavailableSince > DateTime.UtcNow.AddDays(-30))
                        video.NextUpdate = DateTime.UtcNow.AddDays(1);
                    else
                        video.NextUpdate = null;
                }

                video.LastUpdate = DateTime.UtcNow;
                video.NextUpdateId = null;

                if (exists && scanComment)
                    scanCommentMessages.Add(new ScanVideoComment(id));
            }
            else
            {
                _logger.LogWarning("Video {Id} not exists in database", id);
            }
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        await context.PublishBatch(scanCommentMessages, context.CancellationToken);
    }


    [GeneratedRegex(@"\W", RegexOptions.Compiled)]
    private static partial Regex NonWorldRegex();
}