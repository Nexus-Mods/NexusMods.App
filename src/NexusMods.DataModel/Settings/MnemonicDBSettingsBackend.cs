using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Settings;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel.Settings;

/// <summary>
/// A settings backend that uses the MnemonicDB for storage.
/// </summary>
internal sealed class MnemonicDBSettingsBackend : ISettingsStorageBackend
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

    /// <inheritdoc />
    public void Save<T>(T value) where T : class, ISettings, new()
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
        tx.Commit().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "Failed to save settings for `{Type}`", typeof(T));
            }
        );
    }

    /// <inheritdoc />
    public T? Load<T>() where T : class, ISettings, new()
    {
        var settings = Setting.FindByName(_conn.Value.Db, GetId<T>()).ToArray();
        if (!settings.Any()) 
            return null;
        
        return Deserialize<T>(settings.First().Value);
    }

    private static string GetId<T>() where T : ISettings
    {
        return typeof(T).FullName ?? typeof(T).Name;
    }
}
