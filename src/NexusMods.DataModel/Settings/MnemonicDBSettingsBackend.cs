using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Settings;

/// <summary>
/// A settings backend that uses the MnemonicDB for storage.
/// </summary>
internal sealed class MnemonicDBSettingsBackend : IAsyncSettingsStorageBackend
{
    private readonly ILogger<MnemonicDBSettingsBackend> _logger;
    private readonly Lazy<IConnection> _conn;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;

    public MnemonicDBSettingsBackend(ILogger<MnemonicDBSettingsBackend> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;

        _conn = new Lazy<IConnection>(serviceProvider.GetRequiredService<IConnection>);
        _jsonOptions = new Lazy<JsonSerializerOptions>(serviceProvider.GetRequiredService<JsonSerializerOptions>);
    }
    
    public void Dispose()
    {
        // Nothing to dispose
    }

    public SettingsStorageBackendId Id { get; } = Abstractions.Serialization.Settings.Extensions.StorageBackendId;
    
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

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask Save<T>(T value, CancellationToken cancellationToken) where T : class, ISettings, new()
    {
        var db = _conn.Value.Db;
        using var tx = _conn.Value.BeginTransaction();
        EntityId id;
        var settings = Setting.FindByName(db, GetId<T>()).ToArray();
        if (!settings.Any())
        {
            id = tx.TempId();
            tx.Add(id, Setting.Name, GetId<T>());
        }
        else
        {
            id = settings.First().Id;
        }
        
        tx.Add(id, Setting.Value, Serialize(value) ?? "null");
        await tx.Commit();
    }

    /// <inheritdoc />
    public ValueTask<T?> Load<T>(CancellationToken cancellationToken) where T : class, ISettings, new()
    {
        var settings = Setting.FindByName(_conn.Value.Db, GetId<T>()).ToArray();
        if (!settings.Any()) 
            return ValueTask.FromResult<T?>(null);
        return ValueTask.FromResult(Deserialize<T>(settings.First().Value));
    }
}
