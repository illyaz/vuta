namespace VUta.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;

    public class VUtaDbContextFactory : IDesignTimeDbContextFactory<VUtaDbContext>
    {
        public VUtaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<VUtaDbContext>();
            optionsBuilder
                .UseSnakeCaseNamingConvention()
                .UseNpgsql("Host=localhost; Port=35432; Database=vuta_dev; Username=postgres; Password=12345678");

            return new VUtaDbContext(optionsBuilder.Options);
        }
    }
}
