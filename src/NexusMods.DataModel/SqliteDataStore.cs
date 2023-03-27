using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text.Json;
using BitFaster.Caching.Lru;
using Microsoft.Data.Sqlite;
using NexusMods.DataModel.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NexusMods.DataModel.Attributes;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Interprocess.Messages;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;


namespace NexusMods.DataModel;

/// <summary>
/// An implementation of a <see cref="IDataStore"/> backed by an Sqlite 3.X database.
/// </summary>
public class SqliteDataStore : IDataStore, IDisposable
{
    private readonly Dictionary<EntityCategory, string> _getStatements;
    private readonly Dictionary<EntityCategory, string> _putStatements;
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly Dictionary<EntityCategory, string> _casStatements;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;
    private readonly Dictionary<EntityCategory, string> _prefixStatements;
    private readonly Dictionary<EntityCategory, string> _deleteStatements;
    private readonly ConcurrentLru<IId, Entity> _cache;
    private readonly IMessageProducer<RootChange> _rootChangeProducer;
    private readonly IMessageConsumer<RootChange> _rootChangeConsumer;
    private readonly ConcurrentQueue<RootChange> _pendingRootChanges = new();
    private readonly ConcurrentQueue<IdUpdated> _pendingIdPuts = new();
    private readonly CancellationTokenSource _enqueuerTcs;
    private readonly ILogger<SqliteDataStore> _logger;
    private readonly IMessageProducer<IdUpdated> _idPutProducer;
    private readonly IMessageConsumer<IdUpdated> _idPutConsumer;
    private readonly Dictionary<EntityCategory, bool> _immutableFields;
    private readonly ObjectPool<SqliteConnection> _pool;

    /// <summary/>
    /// <param name="logger">Logs events.</param>
    /// <param name="path">Location of the database.</param>
    /// <param name="provider">Dependency injection container.</param>
    /// <param name="rootChangeProducer">Producer of changes to <see cref="Root{TRoot}"/>s</param>
    /// <param name="rootChangeConsumer">Consumer of changes to <see cref="Root{TRoot}"/>s</param>
    /// <param name="idPutProducer">Producer of events for updated IDs.</param>
    /// <param name="idPutConsumer">Consumer of events for updated IDs.</param>
    public SqliteDataStore(ILogger<SqliteDataStore> logger, AbsolutePath path, IServiceProvider provider,
        IMessageProducer<RootChange> rootChangeProducer,
        IMessageConsumer<RootChange> rootChangeConsumer,
        IMessageProducer<IdUpdated> idPutProducer,
        IMessageConsumer<IdUpdated> idPutConsumer)
    {
        _logger = logger;
        var connectionString = string.Intern($"Data Source={path}");
        _pool = ObjectPool.Create(new ConnectionPoolPolicy(connectionString));

        _getStatements = new Dictionary<EntityCategory, string>();
        _putStatements = new Dictionary<EntityCategory, string>();
        _casStatements = new Dictionary<EntityCategory, string>();
        _prefixStatements = new Dictionary<EntityCategory, string>();
        _deleteStatements = new Dictionary<EntityCategory, string>();
        _immutableFields = new Dictionary<EntityCategory, bool>();
        EnsureTables();

        _jsonOptions = new Lazy<JsonSerializerOptions>(provider.GetRequiredService<JsonSerializerOptions>);
        _cache = new ConcurrentLru<IId, Entity>(1000);
        _rootChangeProducer = rootChangeProducer;
        _rootChangeConsumer = rootChangeConsumer;
        _idPutProducer = idPutProducer;
        _idPutConsumer = idPutConsumer;

        _enqueuerTcs = new CancellationTokenSource();
        Task.Run(EnqueuerLoop);
    }

    private async Task EnqueuerLoop()
    {
        while (!_enqueuerTcs.Token.IsCancellationRequested)
        {
            if (_pendingIdPuts.TryDequeue(out var put))
            {
                await _idPutProducer.Write(put, _enqueuerTcs.Token);
                continue;
            }
            if (_pendingRootChanges.TryDequeue(out var change))
            {
                await _rootChangeProducer.Write(change, _enqueuerTcs.Token);
                continue;
            }
            await Task.Delay(100);
        }
    }

    private void EnsureTables()
    {

        using var conn = _pool.RentDisposable();

        using (var pragma = conn.Value.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL";
            pragma.ExecuteNonQuery();
        }

        foreach (var table in Enum.GetValues<EntityCategory>())
        {
            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText =
                $"CREATE TABLE IF NOT EXISTS {table} (Id BLOB PRIMARY KEY, Data BLOB)";
            cmd.ExecuteNonQuery();

            _getStatements[table] =
                $"SELECT Data FROM [{table}] WHERE Id = @id";
            _putStatements[table] =
                $"INSERT OR REPLACE INTO [{table}] (Id, Data) VALUES (@id, @data)";
            _casStatements[table] =
                $"UPDATE [{table}] SET Data = @newData WHERE Data = @oldData AND Id = @id RETURNING *;";
            _prefixStatements[table] =
                $"SELECT Id, Data FROM [{table}] WHERE Id >= @prefix ORDER BY Id ASC";
            _deleteStatements[table] =
                $"DELETE FROM [{table}] WHERE Id = @id";

            var memberInfo =
                typeof(EntityCategory).GetField(Enum.GetName(table)!)!;
            _immutableFields[table] = memberInfo.CustomAttributes.Any(x =>
                x.AttributeType == typeof(ImmutableAttribute));
        }
    }

    /// <inheritdoc />
    public IId Put<T>(T value) where T : Entity
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _putStatements[value.Category];
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, value, _jsonOptions.Value);
        var msBytes = ms.ToArray();
        var hash = new XxHash64Algorithm(0).HashBytes(msBytes);
        var id = new Id64(value.Category, hash);

        cmd.Parameters.AddWithValueUntagged("@id", id);
        cmd.Parameters.AddWithValue("@data", msBytes);

        cmd.ExecuteNonQuery();


        if (!_immutableFields[id.Category])
            _pendingIdPuts.Enqueue(new IdUpdated(IdUpdated.UpdateType.Put, id));
        return id;
    }

    /// <inheritdoc />
    public void Put<T>(IId id, T value) where T : Entity
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _putStatements[value.Category];

        cmd.Parameters.AddWithValueUntagged("@id", id);
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, value, _jsonOptions.Value);
        ms.Position = 0;
        cmd.Parameters.AddWithValue("@data", ms.ToArray());
        cmd.ExecuteNonQuery();

        if (!_immutableFields[id.Category])
            _pendingIdPuts.Enqueue(new IdUpdated(IdUpdated.UpdateType.Put, id));
    }

    /// <inheritdoc />
    public T? Get<T>(IId id, bool canCache) where T : Entity
    {
        if (canCache && _cache.TryGet(id, out var cached))
            return (T)cached;

        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _getStatements[id.Category];

        cmd.Parameters.AddWithValueUntagged("@id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var value = (T?)JsonSerializer.Deserialize<Entity>(reader.GetBlob(0), _jsonOptions.Value);
        if (value == null) return null;
        value.DataStoreId = id;

        if (canCache)
            _cache.AddOrUpdate(id, value);

        return value;
    }

    /// <inheritdoc />
    public bool PutRoot(RootType type, IId oldId, IId newId)
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _putStatements[EntityCategory.Roots];
        cmd.Parameters.AddWithValue("@id", (byte)type);
        var newData = new byte[newId.SpanSize + 1];
        newId.ToTaggedSpan(newData.AsSpan());
        cmd.Parameters.AddWithValue("@data", newData);

        cmd.ExecuteNonQuery();

        _pendingRootChanges.Enqueue(new RootChange { Type = type, From = oldId, To = newId });
        return true;
    }

    /// <inheritdoc />
    public IId? GetRoot(RootType type)
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _getStatements[EntityCategory.Roots];
        cmd.Parameters.AddWithValue("@id", (byte)type);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            return reader.GetId(0);
        }

        return null;
    }

    /// <inheritdoc />
    public byte[]? GetRaw(IId id)
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _getStatements[id.Category];

        cmd.Parameters.AddWithValueUntagged("@id", id);
        using var reader = cmd.ExecuteReader();
        return !reader.Read() ? null : reader.GetBlob(0).ToArray();
    }

    /// <inheritdoc />
    public void PutRaw(IId id, ReadOnlySpan<byte> val)
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _putStatements[id.Category];

        cmd.Parameters.AddWithValueUntagged("@id", id);
        cmd.Parameters.AddWithValue("@data", val.ToArray());
        cmd.ExecuteNonQuery();

        if (!_immutableFields[id.Category])
            _pendingIdPuts.Enqueue(new IdUpdated(IdUpdated.UpdateType.Put, id));

    }

    /// <inheritdoc />
    public void Delete(IId id)
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _deleteStatements[id.Category];
        cmd.Parameters.AddWithValueUntagged("@id", id);
        cmd.ExecuteNonQuery();

        if (!_immutableFields[id.Category])
            _pendingIdPuts.Enqueue(new IdUpdated(IdUpdated.UpdateType.Delete, id));
    }

    /// <inheritdoc />
    public async Task<long> PutRaw(IAsyncEnumerable<(IId Key, byte[] Value)> kvs, CancellationToken token = default)
    {
        var iterator = kvs.GetAsyncEnumerator(token);
        var processed = 0;
        var totalLoaded = 0L;
        var done = false;

        using var conn = _pool.RentDisposable();
        while (true)
        {
            {
                await using var tx = conn.Value.BeginTransaction();

                while (processed < 100)
                {
                    if (!await iterator.MoveNextAsync())
                    {
                        done = true;
                        break;
                    }

                    var (id, val) = iterator.Current;
                    await using var cmd = conn.Value.CreateCommand();
                    cmd.CommandText = _putStatements[id.Category];

                    cmd.Parameters.AddWithValueUntagged("@id", id);
                    cmd.Parameters.AddWithValue("@data", val);
                    await cmd.ExecuteNonQueryAsync(token);
                    processed++;
                }

                if (processed > 0)
                {
                    await tx.CommitAsync(token);
                    totalLoaded += processed;
                }

                if (done)
                    break;
                processed = 0;
            }
        }

        return totalLoaded;

    }

    /// <inheritdoc />
    public IEnumerable<T> GetByPrefix<T>(IId prefix) where T : Entity
    {
        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _prefixStatements[prefix.Category];

        cmd.Parameters.AddWithValueUntagged("@prefix", prefix);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetId(prefix.Category, 0);
            if (!id.IsPrefixedBy(prefix))
            {
                yield break;
            }

            var value = JsonSerializer.Deserialize<Entity>(reader.GetBlob(1), _jsonOptions.Value);
            if (value is not T tc) continue;

            value.DataStoreId = id;
            yield return tc;
        }
    }

    /// <inheritdoc />
    public IObservable<RootChange> RootChanges => _rootChangeConsumer.Messages.SelectMany(WaitTillRootReady);

    /// <inheritdoc />
    public IObservable<IId> IdChanges => _idPutConsumer.Messages.SelectMany(WaitTillPutReady);
    /// <summary>
    /// Sometimes we may get a change notification before the underlying Sqlite database has actually updated the value.
    /// So we wait a bit to make sure the value is actually there before we forward the change.
    /// </summary>
    /// <param name="change"></param>
    /// <returns></returns>
    private async Task<RootChange> WaitTillRootReady(RootChange change)
    {
        var maxCycles = 0;
        while (GetRaw(change.To) == null && maxCycles < 10)
        {
            _logger.LogDebug("Waiting for root change {To} to be ready", change.To);
            await Task.Delay(100);
            maxCycles++;
        }
        _logger.LogDebug("Root change {To} is ready", change.To);
        return change;
    }

    /// <summary>
    /// Sometimes we may get a change notification before the underlying Sqlite database has actually updated the value.
    /// So we wait a bit to make sure the value is actually there before we forward the change.
    /// </summary>
    /// <param name="change"></param>
    /// <returns></returns>
    private async Task<IId> WaitTillPutReady(IdUpdated change)
    {
        var maxCycles = 0;
        while (change.Type != IdUpdated.UpdateType.Delete && GetRaw(change.Id) == null && maxCycles < 10)
        {
            _logger.LogDebug("Waiting for write to {Id} to complete", change.Id);
            await Task.Delay(100);
            maxCycles++;
        }
        _logger.LogTrace("Id {Id} is updated", change.Id);
        return change.Id;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _enqueuerTcs.Dispose();
        if (_pool is IDisposable disposable)
            disposable.Dispose();
    }
}
