namespace VUta.Database.Models
{
    using Microsoft.EntityFrameworkCore;

    [Index(nameof(NextUpdate), nameof(NextUpdateId))]
    [Index(nameof(ChannelId), nameof(PublishDate))]
    public class Video
    {
        public string Id { get; set; } = null!;
        public string ChannelId { get; set; } = null!;
        public virtual Channel Channel { get; set; } = null!;
        public virtual List<Comment> Comments { get; set; } = new();

        public bool IsUta { get; set; }
        public string Title { get; set; } = null!;
        public DateTime PublishDate { get; set; }

        public DateTime? LastCommentScan { get; set; }
        public DateTime? NextCommentScan { get; set; }
        public DateTime? LastUpdate { get; set; }
        public DateTime? NextUpdate { get; set; }
        public Guid? NextUpdateId { get; set; }
    }
}
