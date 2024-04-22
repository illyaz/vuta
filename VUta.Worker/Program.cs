﻿using System.Net;
using System.Text;
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
                            h.ConfigureBatchPublish(x =>
                            {
                                x.Enabled = true;
                                x.Timeout = TimeSpan.FromMilliseconds(2);
                            });
                        });

                        cfg.PrefetchCount = opts.Prefetch;
                        cfg.AutoStart = true;
                    });

                    mt.AddConsumer<ScanChannelVideoConsumer>();
                    mt.AddConsumer<ScanVideoCommentConsumer>();
                    mt.AddConsumer<UpdateChannelConsumer>();
                    mt.AddConsumer<UpdateVideoConsumer>();
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
            })
            .Build();

        host.Run();
    }
}