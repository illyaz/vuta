using Microsoft.EntityFrameworkCore;

namespace VUta.Database.Models;

[Index(nameof(Handle))]
[Index(nameof(LastVideoScan))]
[Index(nameof(NextVideoScan))]
[Index(nameof(LastUpdate))]
[Index(nameof(NextUpdate))]
[Index(nameof(NextUpdateId))]
public class Channel
{
    public string Id { get; set; } = null!;
    public string? Handle { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public long VideoCount { get; set; }
    public long? SubscriberCount { get; set; }
    public string Thumbnail { get; set; } = null!;
    public string? Banner { get; set; }
    public virtual List<Video> Videos { get; set; } = new();

    public DateTime? UnavailableSince { get; set; }
    public DateTime? LastVideoScan { get; set; }
    public DateTime? NextVideoScan { get; set; }
    public DateTime? LastUpdate { get; set; }
    public DateTime? NextUpdate { get; set; }
    public Guid? NextUpdateId { get; set; }
}