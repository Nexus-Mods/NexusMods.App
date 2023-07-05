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


namespace NexusMods.DataModel;

/// <summary>
/// An implementation of a <see cref="IDataStore"/> backed by an Sqlite 3.X database.
/// </summary>
public class SqliteDataStore : IDataStore, IDisposable
{
    private bool _isDisposed;

    private readonly Dictionary<EntityCategory, string> _getStatements;
    private readonly Dictionary<EntityCategory, string> _putStatements;
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly Dictionary<EntityCategory, string> _casStatements;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;
    private readonly Dictionary<EntityCategory, string> _prefixStatements;
    private readonly Dictionary<EntityCategory, string> _deleteStatements;
    private readonly Dictionary<EntityCategory,string> _allIdsStatements;

    private readonly ConcurrentLru<IId, Entity> _cache;
    private readonly ConcurrentQueue<IdUpdated> _pendingIdPuts = new();
    private readonly CancellationTokenSource _enqueuerTcs;
    private readonly ILogger<SqliteDataStore> _logger;
    private readonly IMessageProducer<IdUpdated> _idPutProducer;
    private readonly IMessageConsumer<IdUpdated> _idPutConsumer;
    private readonly Dictionary<EntityCategory, bool> _immutableFields;
    private readonly ObjectPool<SqliteConnection> _pool;
    private readonly ConnectionPoolPolicy _poolPolicy;
    private readonly ObjectPoolDisposable<SqliteConnection> _globalHandle;

    /// <summary/>
    /// <param name="logger">Logs events.</param>
    /// <param name="settings">Datamodel settings</param>
    /// <param name="provider">Dependency injection container.</param>
    /// <param name="idPutProducer">Producer of events for updated IDs.</param>
    /// <param name="idPutConsumer">Consumer of events for updated IDs.</param>
    public SqliteDataStore(ILogger<SqliteDataStore> logger, IDataModelSettings settings, IServiceProvider provider,
        IMessageProducer<IdUpdated> idPutProducer,
        IMessageConsumer<IdUpdated> idPutConsumer)
    {
        _logger = logger;

        string connectionString;
        if (settings.UseInMemoryDataModel)
        {
            var id = Guid.NewGuid().ToString();
            connectionString = string.Intern($"Data Source={id};Mode=Memory;Cache=Shared");
        }
        else
        {
            connectionString = string.Intern($"Data Source={settings.DataStoreFilePath}");
        }

        connectionString = string.Intern(connectionString);

        _poolPolicy = new ConnectionPoolPolicy(connectionString);
        _pool = ObjectPool.Create(_poolPolicy);

        // We do this so that while the app is running we never fully close the DB, this is needed
        // if we're using a in-memory store, as closing the final connection will delete the DB.
        _globalHandle = _pool.RentDisposable();

        _getStatements = new Dictionary<EntityCategory, string>();
        _putStatements = new Dictionary<EntityCategory, string>();
        _allIdsStatements = new Dictionary<EntityCategory, string>();
        _casStatements = new Dictionary<EntityCategory, string>();
        _prefixStatements = new Dictionary<EntityCategory, string>();
        _deleteStatements = new Dictionary<EntityCategory, string>();
        _immutableFields = new Dictionary<EntityCategory, bool>();
        EnsureTables();

        _jsonOptions = new Lazy<JsonSerializerOptions>(provider.GetRequiredService<JsonSerializerOptions>);
        _cache = new ConcurrentLru<IId, Entity>(10000);
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

        foreach (var table in EntityCategoryExtensions.GetValues())
        {
            var tableName = table.ToStringFast();

            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} (Id BLOB PRIMARY KEY, Data BLOB)";
            cmd.ExecuteNonQuery();

            _getStatements[table] = $"SELECT Data FROM [{tableName}] WHERE Id = @id";
            _putStatements[table] = $"INSERT OR REPLACE INTO [{tableName}] (Id, Data) VALUES (@id, @data)";
            _allIdsStatements[table] = $"SELECT Id FROM [{tableName}]";
            _casStatements[table] = $"UPDATE [{tableName}] SET Data = @newData WHERE Data = @oldData AND Id = @id RETURNING *;";
            _prefixStatements[table] = $"SELECT Id, Data FROM [{tableName}] WHERE Id >= @prefix ORDER BY Id ASC";
            _deleteStatements[table] = $"DELETE FROM [{tableName}] WHERE Id = @id";

            var memberInfo = typeof(EntityCategory).GetField(Enum.GetName(table)!)!;
            _immutableFields[table] = memberInfo.CustomAttributes.Any(x => x.AttributeType == typeof(ImmutableAttribute));
        }
    }

    /// <inheritdoc />
    public IId Put<T>(T value) where T : Entity
    {
        var id = ContentHashId(value, out var data);
        PutRaw(id, data);
        return id;
    }

    /// <inheritdoc />
    public void Put<T>(IId id, T value) where T : Entity
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

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
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

        if (canCache && _cache.TryGet(id, out var cached))
            return (T)cached;

        _logger.GetIdOfType(id, typeof(T).Name);
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
    public byte[]? GetRaw(IId id)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

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
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

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
    public bool CompareAndSwap(IId id, ReadOnlySpan<byte> val, ReadOnlySpan<byte> expected)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

        using var conn = _pool.RentDisposable();
        using var transaction = conn.Value.BeginTransaction();

        using (var cmd = conn.Value.CreateCommand())
        {
            cmd.CommandText = _getStatements[id.Category];
            cmd.Parameters.AddWithValueUntagged("@id", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read() && !reader.GetBlob(0).SequenceEqual(expected))
                return false;
        }

        using (var cmd = conn.Value.CreateCommand()) {
            cmd.CommandText = _putStatements[id.Category];
            cmd.Parameters.AddWithValueUntagged("@id", id);
            cmd.Parameters.AddWithValue("@data", val.ToArray());
            cmd.ExecuteNonQuery();
            transaction.Commit();
        }

        if (!_immutableFields[id.Category])
            _pendingIdPuts.Enqueue(new IdUpdated(IdUpdated.UpdateType.Put, id));
        return true;
    }

    /// <inheritdoc />
    public void Delete(IId id)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

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
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

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
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

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
    public IObservable<IId> IdChanges => _idPutConsumer.Messages.SelectMany(WaitTillPutReady);

    /// <inheritdoc />
    public IEnumerable<IId> AllIds(EntityCategory category)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _allIdsStatements[category];
        var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return reader.GetId(category, 0);
        }
    }

    /// <inheritdoc />
    public Id64 ContentHashId<T>(T entity, out byte[] data) where T: Entity
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, entity, _jsonOptions.Value);
        var msBytes = ms.ToArray();
        var hash = new XxHash64Algorithm(0).HashBytes(msBytes);
        var id = new Id64(entity.Category, hash);
        data = msBytes;
        return id;
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
            _logger.WaitingForWriteToId(change.Id);
            await Task.Delay(100);
            maxCycles++;
        }

        _logger.IdIsUpdated(change.Id);
        return change.Id;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        if (disposing)
        {
            _globalHandle.Dispose();
            _enqueuerTcs.Dispose();
            if (_pool is IDisposable disposable)
                disposable.Dispose();
            _poolPolicy.Dispose();
        }
        _isDisposed = true;
    }
}
