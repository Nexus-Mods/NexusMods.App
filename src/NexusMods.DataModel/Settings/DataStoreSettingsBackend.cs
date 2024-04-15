using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Abstractions.Settings;

namespace NexusMods.DataModel.Settings;

[UsedImplicitly]
internal sealed class DataStoreSettingsBackend : ISettingsStorageBackend
{
    private readonly ILogger _logger;
    private readonly Lazy<IDataStore> _dataStore;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;

    public DataStoreSettingsBackend(
        IServiceProvider serviceProvider,
        ILogger<DataStoreSettingsBackend> logger)
    {
        _logger = logger;

        // NOTE(erri120): Lazy to fix circular dependency:
        // SettingsManager -> DataStoreSettingsBackend -> IDataStore -> ISettingsManager
        _dataStore = new Lazy<IDataStore>(serviceProvider.GetRequiredService<IDataStore>);
        _jsonOptions = new Lazy<JsonSerializerOptions>(serviceProvider.GetRequiredService<JsonSerializerOptions>);
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

    public void Save<T>(T value) where T : class, ISettings, new()
    {
        var json = Serialize(value);
        var entity = new DataStoreSettingsBackendEntity
        {
            Value = json ?? "null",
        };

        _dataStore.Value.Put(GetId<T>(), entity);
    }

    public T? Load<T>() where T : class, ISettings, new()
    {
        var entity = _dataStore.Value.Get<DataStoreSettingsBackendEntity>(GetId<T>());
        if (entity is null) return null;

        var value = Deserialize<T>(entity.Value);
        return value;
    }

    private static Id64 GetId<T>() => new(EntityCategory.GlobalSettings, (ulong)typeof(T).GetHashCode());

    public void Dispose() { }
}
