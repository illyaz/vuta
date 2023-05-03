namespace VUta.Database.Models
{
    using Microsoft.EntityFrameworkCore;

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
        public string Thumbnail { get; set; } = null!;
        public virtual List<Video> Videos { get; set; } = new();

        public DateTime? LastVideoScan { get; set; }
        public DateTime? NextVideoScan { get; set; }
        public DateTime? LastUpdate { get; set; }
        public DateTime? NextUpdate { get; set; }
        public Guid? NextUpdateId { get; set; }
    }
}
