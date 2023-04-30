namespace VUta.ESIndexer
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    using NpgsqlTypes;

    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks.Dataflow;

    using VUta.Database;

    public class ESIndexerService : BackgroundService
    {
        private BatchBlock<ESIndexerData> _batchBlock;
        private ActionBlock<ESIndexerData[]> _actionBlock;
        private Timer _batchTriggerTimer;

        private readonly ILogger<ESIndexerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _host;
        private readonly HttpClient _http;
        private readonly Lazy<WALService> _walLazy;
        private readonly TaskCompletionSource _indexerCompletion;
        private WALService _wal => _walLazy.Value;

        public bool WaitShutdown { get; set; } = true;
        public int BatchSize { get; } = 1000;

        public ESIndexerService(
            ILogger<ESIndexerService> logger,
            IServiceProvider serviceProvider,
            IOptions<ESIndexerOptions> options,
            IHostApplicationLifetime host,
            HttpClient http)
        {
            _batchBlock = new(BatchSize, new() { BoundedCapacity = BatchSize });
            _batchBlock.LinkTo(_actionBlock = new(BulkAsync, new() { BoundedCapacity = 1 }));
            _batchTriggerTimer = new(_ => _batchBlock.TriggerBatch());
            _logger = logger;
            _serviceProvider = serviceProvider;
            _host = host;
            _http = http;
            _walLazy = new(serviceProvider.GetRequiredService<WALService>);

            _http.BaseAddress = new Uri(options.Value.Elasticsearch);

            if (!string.IsNullOrEmpty(options.Value.ElasticsearchAuthorization))
                _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", options.Value.ElasticsearchAuthorization);

            _indexerCompletion = new();
        }

        private record AdditionalVideoFields(string ChannelId, DateTime PublishDate, bool IsUta);

        private async Task BulkAsync(ESIndexerData[] items)
        {
            if (!WaitShutdown)
                _host.ApplicationStopping
                    .ThrowIfCancellationRequested();

            try
            {
                var lastLsn = items.Where(x => x.WalEnd != null).Max(x => x.WalEnd);
                _logger.LogInformation("Indexing {Count} searchable object - {Lsn}", items.Length, lastLsn);

                var additionalVideoFields = items
                    .Where(x => x.Index == "comments" && (
                        !x.Data.ContainsKey("channel_id")
                        || !x.Data.ContainsKey("video_publish_date")
                        || !x.Data.ContainsKey("video_is_uta")))
                    .Select(x => (string)x.Data["video_id"]!)
                    .Distinct()
                    .ToDictionary(k => k, _ => default(AdditionalVideoFields));

                if (additionalVideoFields.Any())
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<VUtaDbContext>();
                    additionalVideoFields = (await db.Videos
                        .AsNoTracking()
                        .Where(x => additionalVideoFields.Keys.Contains(x.Id))
                        .ToDictionaryAsync(k => k.Id, v => new AdditionalVideoFields(v.ChannelId, v.PublishDate, v.IsUta)))!;
                }

                _batchTriggerTimer.Change(3000, 3000);

                using var req = new HttpRequestMessage(HttpMethod.Post, "/_bulk");
                var data = new MemoryStream();
                foreach (var item in items)
                {
                    if (item.IsDeleted)
                    {
                        data.Write(JsonSerializer.SerializeToUtf8Bytes(new
                        {
                            delete = new
                            {
                                _index = item.Index,
                                _id = item.Data["id"]
                            }
                        }));
                        data.WriteByte(0x0a);
                    }
                    else
                    {
                        if (item.Index == "comments"
                            && additionalVideoFields.TryGetValue((string)item.Data["video_id"]!, out var additional)
                            && additional != default)
                        {
                            item.Data.TryAdd("channel_id", additional.ChannelId);
                            item.Data.TryAdd("video_publish_date", additional.PublishDate);
                            item.Data.TryAdd("video_is_uta", additional.IsUta);
                        }
                        data.Write(JsonSerializer.SerializeToUtf8Bytes(new
                        {
                            index = new
                            {
                                _index = item.Index,
                                _id = item.Data["id"]
                            }
                        }));
                        data.WriteByte(0x0a);
                        item.Data.Remove("id");

                        data.Write(JsonSerializer.SerializeToUtf8Bytes(item.Data));
                        data.WriteByte(0x0a);
                    }
                }
                data.Position = 0;
                req.Content = new StreamContent(data);
                req.Content!.Headers.ContentType = new("application/json");
                using var res = await _http.SendAsync(req);

                if (res.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var json = await JsonSerializer.DeserializeAsync<JsonElement>(await res.Content.ReadAsStreamAsync());

                    if (json.TryGetProperty("errors", out var jErrors)
                        && jErrors.GetBoolean() == false)
                    {
                        if (lastLsn != null)
                            await _wal.SendStatusUpdateAsync(lastLsn.Value);
                    }
                    else
                    {
                        _logger.LogCritical("Bulk errors\n{json}", json.ToString());
                        _host.StopApplication();
                    }
                }
                else
                    res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _indexerCompletion.TrySetException(ex);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mappings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await File.ReadAllTextAsync("mappings.json"));
            foreach (var (name, map) in mappings!)
            {
                _logger.LogInformation("Creating index {name} if not exists", name);
                using var req = new HttpRequestMessage(HttpMethod.Put, $"/{name}");
                req.Content = JsonContent.Create(map);

                var res = await _http.SendAsync(req, stoppingToken);
                var result = await res.Content.ReadFromJsonAsync<JsonElement>();

                if (res.StatusCode == HttpStatusCode.BadRequest
                    && result.GetProperty("error").GetProperty("type").GetString() == "resource_already_exists_exception")
                    continue;
                else if (res.StatusCode == HttpStatusCode.OK)
                    continue;
                else
                {
                    _logger.LogCritical("Create index error\n{json}", result.ToString());
                    _host.StopApplication();
                    return;
                }
            }

            _batchTriggerTimer.Change(3000, 3000);

            try
            {
                await _indexerCompletion.Task.WaitAsync(stoppingToken);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested) { }

            if (WaitShutdown)
            {
                try { await Task.Delay(Timeout.Infinite, stoppingToken); } catch (TaskCanceledException) { }
                _logger.LogInformation("Flushing indexer");
                _batchBlock.Complete();
                await _batchBlock.Completion;

                _actionBlock.Complete();
                await _actionBlock.Completion;
            }
        }

        public async Task AddDataAsync(ESIndexerData data, CancellationToken cancellation = default)
        {
            if (!await _batchBlock.SendAsync(data, cancellation) && !cancellation.IsCancellationRequested)
                throw new InvalidOperationException("Could not add data to buffer block");
        }
    }

    public record ESIndexerData(
        string Index,
        Dictionary<string, object?> Data,
        NpgsqlLogSequenceNumber? WalEnd,
        bool IsDeleted = false);
}