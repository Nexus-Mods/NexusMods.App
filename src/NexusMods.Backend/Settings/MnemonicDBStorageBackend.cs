using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Settings;

namespace NexusMods.Backend;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
internal class MnemonicDBStorageBackend : IAsyncStorageBackend
{
    public StorageBackendId Id { get; } = StorageBackendId.From(Guid.Parse("6317967a-3ee9-4e3f-bb24-ebbe40560160"));

    private readonly ILogger _logger;
    private readonly Lazy<IConnection> _conn;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;

    public MnemonicDBStorageBackend(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<MnemonicDBStorageBackend>>();

        _conn = new Lazy<IConnection>(serviceProvider.GetRequiredService<IConnection>);
        _jsonOptions = new Lazy<JsonSerializerOptions>(serviceProvider.GetRequiredService<JsonSerializerOptions>);
    }

    private string? Serialize<T>(T value) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions.Value);
            return json;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while serializing `{Type}` with value `{Value}`", typeof(T), value);
            return null;
        }
    }

    private T? Deserialize<T>(string json) where T : class
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(json, _jsonOptions.Value);
            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while deserializing to `{Type}` with JSON `{Json}`", typeof(T), json);
            return null;
        }
    }

    private static string GetId<T>() where T : ISettings
    {
        return typeof(T).FullName ?? typeof(T).Name;
    }

    public async ValueTask Save<T>(T value, CancellationToken cancellationToken) where T : class, ISettings, new()
    {
        var db = _conn.Value.Db;
        using var tx = _conn.Value.BeginTransaction();

        var name = GetId<T>();

        EntityId id;
        var settings = Setting.FindByName(db, name).ToArray();
        if (settings.Length == 0)
        {
            id = tx.TempId();
            tx.Add(id, Setting.Name, name);
        }
        else
        {
            id = settings.First().Id;
        }

        tx.Add(id, Setting.Value, Serialize(value) ?? "null");
        await tx.Commit();
    }

    public ValueTask<T?> Load<T>(CancellationToken cancellationToken) where T : class, ISettings, new()
    {
        var settings = Setting.FindByName(_conn.Value.Db, GetId<T>()).ToArray();
        if (settings.Length == 0) return ValueTask.FromResult<T?>(null);
        return ValueTask.FromResult(Deserialize<T>(settings.First().Value));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
