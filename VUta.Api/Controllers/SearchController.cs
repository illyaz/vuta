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
            [FromQuery] string query,
            [FromQuery, Range(-1, 1)] int sort = 0,
            [FromQuery, Range(0, 199)] int page = 0,
            [FromQuery] bool? isUta = null,
            [FromQuery] string? channelId = null)
        {
            var mustDescriptor = new List<Func<QueryContainerDescriptor<ESComment>, QueryContainer>>();

            if (isUta != null)
                mustDescriptor.Add(q => q.Term(t => t.VideoIsUta, isUta.Value));

            if (channelId != null)
                mustDescriptor.Add(q => q.Term(t => t.ChannelId, channelId));

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
                    .Includes(i => i
                        .Field(f => f.VideoId)
                        .Field(f => f.ChannelId)
                        .Field(f => f.VideoPublishDate)))
                .Query(q => q
                    .Bool(b => b
                        .Must(mustDescriptor.ToArray())))
                .Sort(s =>
                {
                    if (sort == -1)
                        s.Ascending(f => f.VideoPublishDate);
                    else if (sort == 1)
                        s.Descending(f => f.VideoPublishDate);

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
                .Select(x => new { x.Id, x.Title, x.PublishDate, x.LastUpdate, ChannelTitle = x.Channel.Title })
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
                        VideoLastUpdate = videos[hit.Source.VideoId].LastUpdate,
                        HighlightedText = string.Join(string.Empty, hit.Highlight["text"]).Trim()
                    })
            });
        }
    }

    public class ESComment
    {
        public string VideoId { get; set; } = null!;
        public bool VideoIsUta { get; set; }
        public DateTime VideoPublishDate { get; set; }
        public string ChannelId { get; set; } = null!;
        public string Text { get; set; } = null!;
        public int LikeCount { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
