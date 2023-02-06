using System.Buffers.Binary;
using System.Data.SQLite;
using System.Text.Json;
using NexusMods.DataModel.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;


namespace NexusMods.DataModel;

public class SqliteDataStore : IDataStore
{
    private readonly AbsolutePath _path;
    private readonly string _connectionString;
    private readonly SQLiteConnection _conn;
    private readonly Dictionary<EntityCategory,string> _getStatements;
    private readonly Dictionary<EntityCategory,string> _putStatements;
    private readonly Dictionary<EntityCategory,string> _casStatements;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;
    private readonly Dictionary<EntityCategory,string> _prefixStatements;

    public SqliteDataStore(AbsolutePath path, IServiceProvider provider)
    {
        _path = path;
        _connectionString = string.Intern($"Data Source={path}");
        _conn = new SQLiteConnection(_connectionString);
        _conn.Open();
        
        _getStatements = new Dictionary<EntityCategory, string>();
        _putStatements = new Dictionary<EntityCategory, string>();
        _casStatements = new Dictionary<EntityCategory, string>();
        _prefixStatements = new Dictionary<EntityCategory, string>();
        EnsureTables();
        
        _jsonOptions = new Lazy<JsonSerializerOptions>(provider.GetRequiredService<JsonSerializerOptions>);
    }

    private void EnsureTables()
    {
        foreach (var table in Enum.GetValues<EntityCategory>())
        {
            using var cmd = new SQLiteCommand($"CREATE TABLE IF NOT EXISTS {table} (Id BLOB PRIMARY KEY, Data BLOB)", _conn);
            cmd.ExecuteNonQuery();
            
            _getStatements[table] = $"SELECT Data FROM [{table}] WHERE Id = @id";
            _putStatements[table] = $"INSERT OR REPLACE INTO [{table}] (Id, Data) VALUES (@Id, @data)";
            _casStatements[table] =
                $"UPDATE [{table}] SET Data = @newData WHERE Data = @oldData AND Id = @id RETURNING *;";
            _prefixStatements[table] = $"SELECT Id, Data FROM [{table}] WHERE Id >= @prefix ORDER BY Id ASC";
        }
    }
    
    public Id Put<T>(T value) where T : Entity
    {
        using var cmd = new SQLiteCommand(_putStatements[value.Category], _conn); 
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, value, _jsonOptions.Value);
        var msBytes = ms.ToArray();
        var hash = new xxHashAlgorithm(0).HashBytes(msBytes);
        var idBytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(idBytes, hash);
        cmd.Parameters.AddWithValue("@id", idBytes);
        cmd.Parameters.AddWithValue("@data", msBytes);
        
        cmd.ExecuteNonQuery();
        return new Id64(value.Category, hash);
    }

    public void Put<T>(Id id, T value) where T : Entity
    {
        using var cmd = new SQLiteCommand(_putStatements[value.Category], _conn); 
        Span<byte> idSpan = stackalloc byte[id.SpanSize];
        id.ToSpan(idSpan);
        cmd.Parameters.AddWithValue("@Id", idSpan.ToArray());
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, value, _jsonOptions.Value);
        ms.Position = 0;
        cmd.Parameters.AddWithValue("@Data", ms.ToArray());
        cmd.ExecuteNonQuery();
    }

    public T? Get<T>(Id id) where T : Entity
    {
        using var cmd = new SQLiteCommand(_getStatements[id.Category], _conn);
        var idBytes = new byte[id.SpanSize];
        id.ToSpan(idBytes.AsSpan());
        cmd.Parameters.AddWithValue("@id", idBytes);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var blob = reader.GetStream(0);
        return (T?)JsonSerializer.Deserialize<Entity>(blob, _jsonOptions.Value);
    }

    public bool PutRoot(RootType type, Id oldId, Id newId)
    {
        using var cmd = new SQLiteCommand(_putStatements[EntityCategory.Roots], _conn);
        cmd.Parameters.AddWithValue("@id", (byte)type);
        var newData = new byte[newId.SpanSize + 1];
        newId.ToTaggedSpan(newData.AsSpan());
        cmd.Parameters.AddWithValue("@data", newData);

        cmd.ExecuteNonQuery();
        return true;
    }

    public Id? GetRoot(RootType type)
    {
        using var cmd = new SQLiteCommand(_getStatements[EntityCategory.Roots], _conn);
        cmd.Parameters.AddWithValue("@id", (byte)type);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var blob = reader.GetStream(0);
            var bytes = new byte[blob.Length];
            blob.Read(bytes, 0, bytes.Length);
            return Id.FromTaggedSpan(bytes);
        }

        return null;

    }


    public byte[]? GetRaw(Id id)
    {
        using var cmd = new SQLiteCommand(_getStatements[id.Category], _conn);
        var idBytes = new byte[id.SpanSize];
        id.ToSpan(idBytes.AsSpan());
        cmd.Parameters.AddWithValue("@id", idBytes);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        var size = reader.GetBytes(0, 0, null, 0, 0);
        var bytes = new byte[size];
        reader.GetBytes(0, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    public void PutRaw(Id id, ReadOnlySpan<byte> val)
    {
        using var cmd = new SQLiteCommand(_putStatements[id.Category], _conn);
        var idBytes = new byte[id.SpanSize];
        id.ToSpan(idBytes.AsSpan());
        cmd.Parameters.AddWithValue("@id", idBytes);
        cmd.Parameters.AddWithValue("@data", val.ToArray());
        cmd.ExecuteNonQuery();

    }

    public IEnumerable<T> GetByPrefix<T>(Id prefix) where T : Entity
    {
        using var cmd = new SQLiteCommand(_prefixStatements[prefix.Category], _conn);
        var idBytes = new byte[prefix.SpanSize];
        prefix.ToSpan(idBytes.AsSpan());
        cmd.Parameters.AddWithValue("@prefix", idBytes);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetId(prefix.Category, 0);
            if (!id.IsPrefixedBy(prefix))
            {
                yield break;
            }
            
            var blob = reader.GetStream(1);
            var value = JsonSerializer.Deserialize<Entity>(blob, _jsonOptions.Value);
            if (value is T tc)
            {
                yield return tc;
            }
        }
    }

    public IObservable<(Id Id, Entity Entity)> Changes { get; }
}


internal static class SqlExtensions
{
    public static Id GetId(this SQLiteDataReader reader, EntityCategory ent, int column)
    {
        var blob = reader.GetStream(column);
        var bytes = new byte[blob.Length];
        blob.Read(bytes, 0, bytes.Length);
        return Id.FromSpan(ent, bytes);
    }
}