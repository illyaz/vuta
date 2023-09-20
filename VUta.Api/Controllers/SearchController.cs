namespace VUta.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    using Nest;

    using System.ComponentModel.DataAnnotations;

    using VUta.Database;

    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ElasticClient _elastic;
        private readonly VUtaDbContext _db;

        public SearchController(
            ElasticClient elastic,
            VUtaDbContext db)
        {
            _elastic = elastic;
            _db = db;
        }

        [HttpGet("comments")]
        public async Task<IActionResult> CommentsAsync(
            [FromQuery] string? query,
            [FromQuery, Range(-1, 1)] int sort = 0,
            [FromQuery, Range(0, 199)] int page = 0,
            [FromQuery] bool? isUta = null,
            [FromQuery] string? channelId = null)
        {
            var mustDescriptor = new List<Func<QueryContainerDescriptor<ESComment>, QueryContainer>>();

            if (isUta != null)
                mustDescriptor.Add(q => q.Term(t => t.VideoIsUta, isUta.Value));

            if (channelId != null)
            {
                if (channelId.StartsWith('@'))
                {
                    var handle = channelId[1..];
                    var channelIdFromHandle = await _db.Channels
                        .Where(x => x.Handle == handle)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync();

                    if (channelIdFromHandle != null)
                        channelId = channelIdFromHandle;
                }

                mustDescriptor.Add(q => q.Term(t => t.ChannelId, channelId));
            }

            if (!string.IsNullOrEmpty(query))
                mustDescriptor.Add(q => q
                    .MultiMatch(mm => mm
                    .Query(query)
                    .Type(TextQueryType.Phrase)
                    .Fields(fs => fs
                        .Field(f => f.Text)
                        .Field(f => f.Text.Suffix("trigram")))));

            var result = await _elastic.SearchAsync<ESComment>(d => d
                .From(50 * page)
                .Size(50)
                .Source(s => s
                    .Includes(i =>
                    {
                        i
                            .Field(f => f.VideoId)
                            .Field(f => f.ChannelId)
                            .Field(f => f.VideoPublishDate);

                        if (string.IsNullOrEmpty(query))
                            i.Field(f => f.Text);
                        return i;
                    }))
                .Query(q => q
                    .Bool(b => b
                        .Must(mustDescriptor.ToArray())))
                .Sort(s =>
                {
                    if (sort == -1)
                        s.Ascending(f => f.VideoPublishDate);
                    else if (sort == 1)
                        s.Descending(f => f.VideoPublishDate);
                    else if (sort == -2)
                        s.Ascending(f => f.VideoViewCount);
                    else if (sort == 2)
                        s.Descending(f => f.VideoViewCount);

                    return s.Field(new("_score"), SortOrder.Descending);
                })
                .Highlight(h => h
                    .Fields(fs => fs
                        .Field(f => f.Text)
                        .MatchedFields(mfs => mfs
                            .Field(f => f.Text)
                            .Field(f => f.Text.Suffix("trigram")))
                        .Type(HighlighterType.Fvh)
                        .FragmentSize(10000)
                        .NumberOfFragments(1000))));

            if (!result.IsValid)
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            var hitVideoIds = result.Hits
                .Select(x => x.Source.VideoId)
                .Distinct()
                .ToArray();

            var videos = hitVideoIds.Any() ? (await _db.Videos
                .AsNoTracking()
                .Where(x => hitVideoIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Title, x.PublishDate, x.ViewCount, x.LastUpdate, ChannelTitle = x.Channel.Title })
                .ToDictionaryAsync(k => k.Id, v => v)) : new();

            return Ok(new
            {
                Took = result.Took,
                Total = result.Total,
                Hits = result.Hits
                    .Select(hit => new
                    {
                        Id = hit.Id,
                        VideoId = hit.Source.VideoId,
                        ChannelId = hit.Source.ChannelId,
                        ChannelTitle = videos[hit.Source.VideoId].ChannelTitle,
                        VideoTitle = videos[hit.Source.VideoId].Title,
                        VideoPublishDate = videos[hit.Source.VideoId].PublishDate,
                        VideoViewCount = videos[hit.Source.VideoId].ViewCount,
                        VideoLastUpdate = videos[hit.Source.VideoId].LastUpdate,
                        HighlightedText = hit.Highlight.ContainsKey("text")
                            ? string.Join(string.Empty, hit.Highlight["text"]).Trim()
                            : hit.Source.Text.Trim()
                    })
            });
        }

        [HttpGet("channels")]
        public async Task<IActionResult> ChannelsAsync(
            [FromQuery] string query,
            [FromQuery, Range(0, 399)] int page = 0)
        {
            var mustDescriptor = new List<Func<QueryContainerDescriptor<ESChannel>, QueryContainer>>();
            var shouldDescriptor = new List<Func<QueryContainerDescriptor<ESChannel>, QueryContainer>>();

            mustDescriptor.Add(q => q
                .MultiMatch(mm => mm
                .Query(query)
                .Fields(fs => fs
                    .Field(f => f.Title)
                    .Field(f => f.Title.Suffix("trigram")))));

            shouldDescriptor.Add(q => q
                .MultiMatch(mm => mm
                .Query(query)
                .Fields(fs => fs
                    .Field(f => f.Title))
                .Type(TextQueryType.PhrasePrefix)));

            var result = await _elastic.SearchAsync<ESChannel>(d => d
                .From(25 * page)
                .Size(25)
                .Source(true)
                .Query(q => q
                    .Bool(b => b
                        .Must(mustDescriptor.ToArray())
                        .Should(shouldDescriptor.ToArray())))
                .Sort(f => f.Field(new("_score"), SortOrder.Descending)));

            return Ok(new
            {
                Took = result.Took,
                Total = result.Total,
                Hits = result.Hits
                    .Select(hit => new
                    {
                        Id = hit.Id,
                        Title = hit.Source.Title,
                        Description = hit.Source.Description,
                        VideoCount = long.Parse(hit.Source.VideoCount),
                        SubscriberCount = hit.Source.SubscriberCount == null ? null as long? : long.Parse(hit.Source.SubscriberCount),
                        Thumbnail = hit.Source.Thumbnail.Replace("=s900", "=s96"),
                        Banner = hit.Source.Banner?.Replace("=w2560", "=w1060")
                    })
            });
        }
    }

    public class ESComment
    {
        public string VideoId { get; set; } = null!;
        public bool VideoIsUta { get; set; }
        public long VideoViewCount { get; set; }
        public DateTime VideoPublishDate { get; set; }
        public string ChannelId { get; set; } = null!;
        public string Text { get; set; } = null!;
        public int LikeCount { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class ESChannel
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string VideoCount { get; set; } = null!;
        public string? SubscriberCount { get; set; }
        public string Thumbnail { get; set; } = null!;
        public string? Banner { get; set; } = null!;
        public DateTime? LastUpdate { get; set; }
    }
}
