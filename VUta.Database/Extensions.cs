namespace VUta.Database
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.Extensions.DependencyInjection;

    using System.Linq;

    public static class Extensions
    {
        public static LockingClauseQueryBuilder<T> For<T>(this IQueryable<T> query)
            => new(query);

        public static IServiceCollection AddVUtaDbContext(this IServiceCollection services,
            string? connectionString,
            Action<DbContextOptionsBuilder>? optionsAction = null)
        {
            services.AddDbContext<VUtaDbContext>(opts =>
            {
                opts.UseNpgsql(connectionString);
                opts.UseSnakeCaseNamingConvention();
                opts.ReplaceService<IQuerySqlGeneratorFactory, VUtaNpgsqlQuerySqlGeneratorFactory>();
#if DEBUG
                opts.EnableSensitiveDataLogging();
#endif
                if (optionsAction != null)
                    optionsAction(opts);
            });

            return services;
        }
    }
}
