﻿using System.Text.RegularExpressions;
using MassTransit;
using VUta.Database;
using VUta.Transport.Messages;
using YoutubeExplode;
using YoutubeExplode.Exceptions;

namespace VUta.Worker.Consumers;

public partial class UpdateVideoConsumer
    : IConsumer<UpdateVideo>
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
    private readonly YoutubeClient _youtube;

    public UpdateVideoConsumer(
        ILogger<UpdateVideoConsumer> logger,
        VUtaDbContext db,
        YoutubeClient youtube)
    {
        _logger = logger;
        _db = db;
        _youtube = youtube;
    }

    public async Task Consume(ConsumeContext<UpdateVideo> context)
    {
        var (id, scanComment) = context.Message;
        var video = await _db.Videos.FindAsync(id, context.CancellationToken);
        var exists = false;
        if (video?.NextUpdateId != context.CorrelationId)
        {
            _logger.LogWarning("Update id not matched", id);
            return;
        }

        if (video != null)
        {
            try
            {
                var videoMeta = await _youtube.Videos
                    .GetMetadataAsync(id, context.CancellationToken);

                var replacedTitle = NonWorldRegex().Replace(video.Title, string.Empty);
                video.Title = videoMeta.Title;
                video.IsUta = _utaList.Any(replacedTitle.Contains);
                video.ViewCount = videoMeta.Engagement.ViewCount;
                video.PublishDate = videoMeta.UploadDate.DateTime.ToUniversalTime();

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
            catch (VideoUnavailableException)
            {
                video.UnavailableSince ??= DateTime.UtcNow;
                if (video.UnavailableSince > DateTime.UtcNow.AddDays(-30))
                    video.NextUpdate = DateTime.UtcNow.AddDays(1);
                else
                    video.NextUpdate = null;
            }

            video.LastUpdate = DateTime.UtcNow;
            video.NextUpdateId = null;

            await _db.SaveChangesAsync(context.CancellationToken);

            if (exists && scanComment)
                await context.Publish(new ScanVideoComment(id), context.CancellationToken);
        }
        else
        {
            _logger.LogWarning("Video {Id} not exists in database", id);
        }
    }

    [GeneratedRegex(@"\W", RegexOptions.Compiled)]
    private static partial Regex NonWorldRegex();
}