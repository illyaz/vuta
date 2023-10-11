namespace VUta.Database
{
    using Microsoft.EntityFrameworkCore;

    using VUta.Database.Models;

    public class VUtaDbContext : DbContext
    {
        public VUtaDbContext(DbContextOptions options)
            : base(options) { }

        public DbSet<Channel> Channels { get; set; } = null!;
        public DbSet<Video> Videos { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
    }
}
