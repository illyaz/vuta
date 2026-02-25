using Elasticsearch.Net;
using Humanizer;
using MassTransit;
using Microsoft.Extensions.Options;
using Nest;
using Serilog;
using VUta.Api.AuthHandlers;
using VUta.Api.Controllers;
using VUta.Database;
using VUta.Transport;

namespace VUta.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.Local.json", true, true);
        builder.Host.UseSerilog((host, log) => log.ReadFrom.Configuration(host.Configuration, "Logging"));

        builder.Services.AddOptions<RabbitMQOptions>()
            .BindConfiguration(RabbitMQOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<ElasticsearchOptions>()
            .BindConfiguration(ElasticsearchOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<ApiOptions>()
            .BindConfiguration(ApiOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(s =>
        {
            var opts = s.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            var settings = new ConnectionSettings(opts.Host);

            settings.DefaultFieldNameInferrer(InflectorExtensions.Underscore);
#if DEBUG
            settings.EnableDebugMode();
#endif
            settings.DefaultMappingFor<ESComment>(i => i.IndexName("comments"));
            settings.DefaultMappingFor<ESChannel>(i => i.IndexName("channels"));

            if (!string.IsNullOrEmpty(opts.ApiKey))
                settings.ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(opts.ApiKey));
            else if (!string.IsNullOrEmpty(opts.Username) && !string.IsNullOrEmpty(opts.Password))
                settings.BasicAuthentication(opts.Username, opts.Password);

            return new ElasticClient(settings);
        });

        builder.Services.AddVUtaDbContext(builder.Configuration.GetConnectionString("Default"));
        builder.Services.AddMassTransit(mt =>
        {
            VUtaConfigurator.Configure(mt);
            mt.UsingRabbitMq((context, cfg) =>
            {
                var opts = context.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
                VUtaConfigurator.Configure(context, cfg);
                cfg.Host(opts.Host, opts.VirtualHost, h =>
                {
                    h.Username(opts.Username ?? "guest");
                    h.Password(opts.Password ?? "guest");
                });

                cfg.AutoStart = true;
            });
        });

        builder.Services.AddHttpClient();
        builder.Services.AddControllers();
        builder.Services.AddRequestDecompression();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services
            .AddAuthentication("VUta")
            .AddScheme<VUtaAuthSchemeOptions, VUtaAuthHandler>("VUta", o =>
            {
                o.Secret = builder.Configuration
                    .GetSection(ApiOptions.Section)
                    .GetValue(nameof(ApiOptions.Secret), Guid.NewGuid().ToString())!;
            });

        builder.Services
            .AddCors(o => o.AddDefaultPolicy(p => p
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod()));

        var app = builder.Build();
        app.UseCors();
        app.UseSerilogRequestLogging();
        app.UseRequestDecompression();

        // Configure the HTTP request pipeline.

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}