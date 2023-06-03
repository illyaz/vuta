namespace VUta.ESIndexer
{
    using Microsoft.Extensions.Options;

    using Npgsql.Replication;
    using Npgsql.Replication.PgOutput;
    using Npgsql.Replication.PgOutput.Messages;

    using NpgsqlTypes;

    using System.Collections.Generic;
    using System.Threading;

    public class WALService : BackgroundService
    {
        private readonly ILogger<WALService> _logger;
        private readonly ESIndexerService _indexer;
        private readonly ESIndexerOptions _options;
        private LogicalReplicationConnection? _connection;

        private static Func<ReplicationValue, ValueTask<object?>> _dateTimeParse = async v =>
        {
            if (v.IsDBNull)
                return null;

            var str = await v.Get<string>();
            if (str == "-infinity")
                return DateTime.MinValue;

            return DateTime.Parse(str);
        };

        private readonly Dictionary<string, Func<ReplicationValue, ValueTask<object?>>> _idFields = new()
        {
            { "id", async v => await v.Get<string>() }
        };

        private readonly Dictionary<string, Func<ReplicationValue, ValueTask<object?>>> _videoFields = new()
        {
            { "id", async v => await v.Get<string>() },
            { "channel_id", async v => await v.Get<string>() },
            { "title", async v => await v.Get<string>() },
            { "publish_date", _dateTimeParse },
            { "last_update", _dateTimeParse }
        };

        private readonly Dictionary<string, Func<ReplicationValue, ValueTask<object?>>> _channelFields = new()
        {
            { "id", async v => await v.Get<string>() },
            { "title", async v => await v.Get<string>() },
            { "description", async v => v.IsDBNull ? null : await v.Get<string>() },
            { "video_count", async v => await v.Get<string>() },
            { "subscriber_count", async v => v.IsDBNull ? null : await v.Get<string>() },
            { "thumbnail", async v => await v.Get<string>() },
            { "banner", async v => v.IsDBNull ? null : await v.Get<string>() },
            { "last_update", _dateTimeParse }
        };

        private readonly Dictionary<string, Func<ReplicationValue, ValueTask<object?>>> _commentFields = new()
        {
            { "id", async v => await v.Get<string>() },
            { "video_id", async v => await v.Get<string>() },
            { "text", async v => await v.Get<string>() },
            { "like_count", async v => await v.Get<string>() },
            { "last_update", _dateTimeParse }
        };

        public WALService(
            ILogger<WALService> logger,
            IOptions<ESIndexerOptions> options,
            ESIndexerService indexer)
        {
            _logger = logger;
            _options = options.Value;
            _indexer = indexer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting LogicalReplication({Replication}, {Publication})", _options.Replication, _options.Publication);
            await using var conn = _connection = new LogicalReplicationConnection(_options.ConnectionString)
            {
                WalReceiverStatusInterval = Timeout.InfiniteTimeSpan
            };
            await conn.Open();

            var slot = new PgOutputReplicationSlot(_options.Replication);

            await foreach (var message in conn.StartReplication(
                slot, new PgOutputReplicationOptions(_options.Publication, 1), stoppingToken))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("WAL Received: [{ServerClock}] {Start} {Type}", message.ServerClock, message.WalStart, message.GetType().Name);

                switch (message)
                {
                    case InsertMessage insert when insert.Relation.Namespace == "public":
                        if (insert.Relation.RelationName == "videos")
                            await _indexer.AddDataAsync(new("videos", await ParseAsync(_videoFields, insert.Relation, insert.NewRow), message.WalEnd), stoppingToken);
                        else if (insert.Relation.RelationName == "channels")
                            await _indexer.AddDataAsync(new("channels", await ParseAsync(_channelFields, insert.Relation, insert.NewRow), message.WalEnd), stoppingToken);
                        else if (insert.Relation.RelationName == "comments")
                            await _indexer.AddDataAsync(new("comments", await ParseAsync(_commentFields, insert.Relation, insert.NewRow), message.WalEnd), stoppingToken);
                        break;
                    case UpdateMessage update when update.Relation.Namespace == "public":
                        if (update.Relation.RelationName == "videos")
                            await _indexer.AddDataAsync(new("videos", await ParseAsync(_videoFields, update.Relation, update.NewRow), message.WalEnd), stoppingToken);
                        else if (update.Relation.RelationName == "channels")
                            await _indexer.AddDataAsync(new("channels", await ParseAsync(_channelFields, update.Relation, update.NewRow), message.WalEnd), stoppingToken);
                        else if (update.Relation.RelationName == "comments")
                            await _indexer.AddDataAsync(new("comments", await ParseAsync(_commentFields, update.Relation, update.NewRow), message.WalEnd), stoppingToken);
                        break;
                    case KeyDeleteMessage delete when delete.Relation.Namespace == "public":
                        if (delete.Relation.RelationName == "videos")
                            await _indexer.AddDataAsync(new("videos", await ParseAsync(_idFields, delete.Relation, delete.Key), message.WalEnd, true), stoppingToken);
                        else if (delete.Relation.RelationName == "channels")
                            await _indexer.AddDataAsync(new("channels", await ParseAsync(_idFields, delete.Relation, delete.Key), message.WalEnd, true), stoppingToken);
                        else if (delete.Relation.RelationName == "comments")
                            await _indexer.AddDataAsync(new("comments", await ParseAsync(_idFields, delete.Relation, delete.Key), message.WalEnd, true), stoppingToken);
                        break;
                }
            }
        }

        public async Task SendStatusUpdateAsync(
            NpgsqlLogSequenceNumber lsn,
            CancellationToken cancellationToken = default)
        {
            if (_connection == null)
                throw new InvalidOperationException();

            _connection.SetReplicationStatus(lsn);
            await _connection.SendStatusUpdate(cancellationToken);
        }

        private static async Task<Dictionary<string, object?>> ParseAsync(
            IReadOnlyDictionary<string, Func<ReplicationValue, ValueTask<object?>>> fields,
            RelationMessage relation,
            ReplicationTuple rows)
        {
            var colMap = relation.Columns
                .Select((col, i) => (col.ColumnName, i))
                .ToDictionary(k => k.i, v => v.ColumnName);

            var data = new Dictionary<string, object?>();
            await foreach (var (row, i) in rows.Select((row, i) => (row, i)))
            {
                var colName = relation.Columns[i].ColumnName;

                if (fields.TryGetValue(colName, out var parser))
                    data.Add(colName, await parser(row));
            }

            if (data.Count != fields.Count)
                throw new InvalidOperationException($"Expected field not exists ({string.Join(",", fields.Keys.Except(data.Keys))})");

            return data;
        }
    }
}