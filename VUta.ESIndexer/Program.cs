namespace VUta.ESIndexer
{
    using Serilog;

    using System.Text;

    using VUta.Database;

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            IHost host = Host.CreateDefaultBuilder(args)
                .UseSerilog((host, log) => log.ReadFrom.Configuration(host.Configuration, "Logging"))
                .ConfigureServices((host, services) =>
                {
                    services.AddOptions<ESIndexerOptions>()
                        .BindConfiguration(ESIndexerOptions.Section)
                        .ValidateDataAnnotations()
                        .ValidateOnStart();

                    services
                        .AddVUtaDbContext(host.Configuration
                            .GetSection(ESIndexerOptions.Section)
                            .GetValue<string>("ConnectionString"))
                        .AddHttpClient()
                        .AddSingleton<WALService>()
                        .AddSingleton<ESIndexerService>()
                        .AddHostedService(s => s.GetRequiredService<ESIndexerService>());

                    if (args.Contains("--sync"))
                    {
                        services
                            .AddSingleton<SyncService>()
                            .AddHostedService(s => s.GetRequiredService<SyncService>());
                    }
                    else
                    {
                        services.AddHostedService(s => s.GetRequiredService<WALService>());
                    }
                })
                .Build();

            host.Run();
        }
    }
}