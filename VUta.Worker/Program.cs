using System.Net;
using System.Text;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MassTransit;
using Microsoft.Extensions.Options;
using Serilog;
using VUta.Database;
using VUta.Transport;
using VUta.Worker.Consumers;
using VUta.Worker.Services;
using YoutubeExplode;

namespace VUta.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog((host, log) => log.ReadFrom.Configuration(host.Configuration, "Logging"))
            .ConfigureAppConfiguration(c => c.AddJsonFile("appsettings.Local.json", true, true))
            .ConfigureServices((host, services) =>
            {
                services.AddVUtaDbContext(host.Configuration.GetConnectionString("Default"));
                services.AddOptions<MassTransitHostOptions>()
                    .Configure(o => o.WaitUntilStarted = true);

                services.AddOptions<WorkerOptions>()
                    .BindConfiguration(WorkerOptions.Section)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddMassTransit(mt =>
                {
                    VUtaConfigurator.Configure(mt);
                    mt.UsingRabbitMq((context, cfg) =>
                    {
                        var opts = context.GetRequiredService<IOptions<WorkerOptions>>().Value;
                        VUtaConfigurator.Configure(context, cfg);
                        cfg.Host(opts.Host, opts.VirtualHost, h =>
                        {
                            h.Username(opts.Username ?? "guest");
                            h.Password(opts.Password ?? "guest");
                        });

                        cfg.PrefetchCount = opts.Prefetch;
                        cfg.AutoStart = true;
                    });

                    mt.AddConsumer<ScanChannelVideoConsumer>();
                    mt.AddConsumer<ScanVideoCommentConsumer>();
                    mt.AddConsumer<UpdateChannelConsumer>();
                    mt.AddConsumer<UpdateVideoConsumer>(c => c
                        .Options<BatchOptions>(o => o
                            .SetTimeLimit(TimeSpan.FromSeconds(1))
                            .SetMessageLimit(25)
                            .SetConcurrencyLimit(10)));
                    mt.AddConsumer<AddChannelConsumer>();
                });

                services.AddSingleton(new YoutubeClient(
                    new HttpClient(new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.All
                    })));

                services.AddHostedService<VideoNextUpdateProducerBackgroundService>();
                services.AddHostedService<VideoNextUpdateStuckProducerBackgroundService>();
                services.AddHostedService<ChannelNextUpdateProducerBackgroundService>();
                services.AddHostedService<ChannelNextUpdateStuckProducerBackgroundService>();
                services.AddSingleton<VUtaHttpClientFactory>();
                services.AddSingleton(s => new YouTubeService(new BaseClientService.Initializer
                {
                    ApiKey = s.GetRequiredService<IOptions<WorkerOptions>>().Value.YoutubeApiKey,
                    HttpClientFactory = s.GetRequiredService<VUtaHttpClientFactory>()
                }));
            })
            .Build();

        host.Run();
    }
}