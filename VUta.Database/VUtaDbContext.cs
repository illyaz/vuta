namespace VUta.Database
{
    using Microsoft.EntityFrameworkCore;

    using VUta.Database.Models;

    public class VUtaDbContext : DbContext
    {
        public VUtaDbContext(DbContextOptions options)
            : base(options) { }

        public DbSet<Channel> Channels { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<Comment> Comments { get; set; }
    }
}
