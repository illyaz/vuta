using Microsoft.EntityFrameworkCore;

namespace VUta.Database.Models;

[Index(nameof(LastUpdate))]
public class Comment
{
    public string Id { get; set; } = null!;
    public string VideoId { get; set; } = null!;
    public virtual Video Video { get; set; } = null!;
    public string Text { get; set; } = null!;
    public long LikeCount { get; set; }
    public string? RepliesId { get; set; }
    public DateTime LastUpdate { get; set; }
}