using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using BitFaster.Caching.Lru;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Attributes;
using NexusMods.DataModel.Extensions;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel;

/// <summary>
/// An implementation of a <see cref="IDataStore"/> backed by an Sqlite 3.X database.
/// </summary>
public class SqliteDataStore : IDataStore, IDisposable
{
    // TODO: Hello, I'm sure you'll be reading this sometime while doing event sourcing.
    // Can you make these dictionaries as arrays when porting to RocksDB?
    // These are finite size we know at compile time, we don't replace the statements,
    // so using dicts just slows down all of our database hits.

    private bool _isDisposed;
    private readonly object _writerLock;

    private readonly Dictionary<EntityCategory, string> _getAllStatements;
    private readonly Dictionary<EntityCategory, string> _getStatements;
    private readonly Dictionary<EntityCategory, string> _putStatements;
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly Dictionary<EntityCategory, string> _casStatements;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;
    private readonly Dictionary<EntityCategory, string> _prefixStatements;
    private readonly Dictionary<EntityCategory, string> _deleteStatements;
    private readonly Dictionary<EntityCategory,string> _allIdsStatements;

    private readonly ConcurrentLru<IId, Entity> _cache;
    private readonly ILogger<SqliteDataStore> _logger;
    private readonly Dictionary<EntityCategory, bool> _immutableFields;
    private readonly ObjectPool<SqliteConnection> _pool;
    private readonly ConnectionPoolPolicy _poolPolicy;
    private readonly ObjectPoolDisposable<SqliteConnection> _globalHandle;
    private readonly Subject<IId> _updatedIds;

    /// <summary/>
    /// <param name="logger">Logs events.</param>
    /// <param name="settings">Datamodel settings</param>
    /// <param name="provider">Dependency injection container.</param>
    public SqliteDataStore(ILogger<SqliteDataStore> logger, IDataModelSettings settings, IServiceProvider provider)
    {
        _logger = logger;
        _writerLock = new object();

        string connectionString;
        if (settings.UseInMemoryDataModel)
        {
            var id = Guid.NewGuid().ToString();
            connectionString = string.Intern($"Data Source={id};Mode=Memory;Cache=Shared");
            _logger.LogDebug("Using in-memory sqlite data store");
        }
        else
        {
            var path = settings.DataStoreFilePath.ToAbsolutePath();
            if (!path.Parent.DirectoryExists())
                path.Parent.CreateDirectory();

            connectionString = string.Intern($"Data Source={settings.DataStoreFilePath}");
            _logger.LogDebug("Using sqlite data store at {DataSource}", settings.DataStoreFilePath);
        }

        connectionString = string.Intern(connectionString);

        _poolPolicy = new ConnectionPoolPolicy(connectionString);
        _pool = ObjectPool.Create(_poolPolicy);

        // We do this so that while the app is running we never fully close the DB, this is needed
        // if we're using a in-memory store, as closing the final connection will delete the DB.
        _globalHandle = _pool.RentDisposable();

        _getAllStatements = new Dictionary<EntityCategory, string>();
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

        _updatedIds = new Subject<IId>();
    }

    private void EnsureTables()
    {
        using var conn = _pool.RentDisposable();

        using (var pragma = conn.Value.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode = WAL";
            pragma.ExecuteNonQuery();
        }
        
        using (var pragma = conn.Value.CreateCommand())
        {
            pragma.CommandText = "PRAGMA synchronous = NORMAL";
            pragma.ExecuteNonQuery();
        }

        foreach (var table in EntityCategoryExtensions.GetValues())
        {
            var tableName = table.ToStringFast();

            using var cmd = conn.Value.CreateCommand();
            cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {tableName} (Id BLOB PRIMARY KEY, Data BLOB)";
            cmd.ExecuteNonQuery();

            _getAllStatements[table] = $"SELECT Id,Data FROM [{tableName}]";
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
    public Id64[] PutAll<TK, TV>(Span<KeyValuePair<TK, TV>> values) where TV : Entity
    {
        using var conn = _pool.RentDisposable();
        var ids = GC.AllocateUninitializedArray<Id64>(values.Length);
        using var tx = conn.Value.BeginTransaction();
        for (var x = 0; x < values.Length; x++)
        {
            ids[x] = ContentHashId(values[x].Value, out var data);
            PutRawItem(ids[x], data, conn);
        }
        
        tx.Commit();
        
        foreach (var id in ids)
            NotifyOfUpdatedId(id);

        return ids;
    }
    
    /// <inheritdoc />
    public Id64[] PutAll<T>(Span<T> values) where T : Entity
    {
        using var conn = _pool.RentDisposable();
        var ids = GC.AllocateUninitializedArray<Id64>(values.Length);
        using var tx = conn.Value.BeginTransaction();
        for (var x = 0; x < values.Length; x++)
        {
            var value = values[x];
            ids[x] = ContentHashId(value, out var data);
            PutRawItem(ids[x], data, conn);
        }

        tx.Commit();
        foreach (var id in ids)
            NotifyOfUpdatedId(id);

        return ids;
    }

    /// <inheritdoc />
    public void PutAll<T>(Span<(IId id, T value)> items)  where T : Entity
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

        using var conn = _pool.RentDisposable();
        using var tx = conn.Value.BeginTransaction();
        foreach (var item in items)
            PutOneItem(item.id, item.value, conn);

        tx.Commit();

        // We notify after DB has the item.
        foreach (var item in items)
            NotifyOfUpdatedId(item.id);
    }

    /// <inheritdoc />
    public void PutAllRaw(Span<(IId id, byte[] value)> items)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

        using var conn = _pool.RentDisposable();
        using var tx = conn.Value.BeginTransaction();
        foreach (var item in items)
            PutRawItem(item.id, item.value, conn);

        tx.Commit();

        // We notify after DB has the item.
        foreach (var item in items)
            NotifyOfUpdatedId(item.id);
    }
    
    /// <inheritdoc />
    public void Put<T>(IId id, T value) where T : Entity
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

        using var conn = _pool.RentDisposable();
        PutOneItem(id, value, conn);
        NotifyOfUpdatedId(id);
    }

    private void NotifyOfUpdatedId(IId id)
    {
        try
        {
            if (!_immutableFields[id.Category])
                _updatedIds.OnNext(id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to notify of updated id {Id}", id);
        }
    }

    /// <inheritdoc />
    public T? Get<T>(IId id, bool canCache) where T : Entity
    {
        if (id is null)
            throw new ArgumentNullException(nameof(id));

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
    public IEnumerable<T> GetAll<T>(EntityCategory category) where T : Entity
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SqliteDataStore));

        using var conn = _pool.RentDisposable();
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _getAllStatements[category];

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetId(category, 0);

            var value = JsonSerializer.Deserialize<Entity>(reader.GetBlob(1), _jsonOptions.Value);
            if (value is not T tc)
                continue;

            value.DataStoreId = id;
            yield return tc;
        }
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
        PutRawItem(id, val, conn);
        NotifyOfUpdatedId(id);
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
            lock (_writerLock)
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        NotifyOfUpdatedId(id);
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
        lock (_writerLock)
        {
            cmd.ExecuteNonQuery();
        }

        NotifyOfUpdatedId(id);
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
    public IObservable<IId> IdChanges => _updatedIds.SelectMany(WaitTillPutReady);

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
        var hash = XxHash64Algorithm.HashBytes(msBytes);
        var id = new Id64(entity.Category, hash);
        data = msBytes;
        return id;
    }

    /// <summary>
    /// Sometimes we may get a change notification before the underlying Sqlite database has actually updated the value.
    /// So we wait a bit to make sure the value is actually there before we forward the change.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private async Task<IId> WaitTillPutReady(IId id)
    {
        var maxCycles = 0;
        while (GetRaw(id) == null && maxCycles < 10)
        {
            _logger.WaitingForWriteToId(id);
            await Task.Delay(100);
            maxCycles++;
        }

        _logger.IdIsUpdated(id);
        return id;
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
            if (_pool is IDisposable disposable)
                disposable.Dispose();
            _poolPolicy.Dispose();
        }
        _isDisposed = true;
    }
    
    private void PutRawItem(IId id, ReadOnlySpan<byte> val, ObjectPoolDisposable<SqliteConnection> conn)
    {
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _putStatements[id.Category];
        cmd.Parameters.AddWithValueUntagged("@id", id);
        cmd.Parameters.AddWithValue("@data", val.ToArray());
        lock (_writerLock)
        {
            cmd.ExecuteNonQuery();
        }
    }

    private void PutOneItem<T>(IId id, T value, ObjectPoolDisposable<SqliteConnection> conn) where T : Entity
    {
        using var cmd = conn.Value.CreateCommand();
        cmd.CommandText = _putStatements[value.Category];

        cmd.Parameters.AddWithValueUntagged("@id", id);
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, value, _jsonOptions.Value);
        ms.Position = 0;
        cmd.Parameters.AddWithValue("@data", ms.ToArray());
        lock (_writerLock)
        {
            cmd.ExecuteNonQuery();
        }
    }
}
