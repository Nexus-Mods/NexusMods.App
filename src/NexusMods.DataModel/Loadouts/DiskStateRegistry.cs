using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using Reloaded.Memory.Extensions;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public class DiskStateRegistry : IDiskStateRegistry
{
    private readonly ILogger<DiskStateRegistry> _logger;
    private readonly IDataStore _dataStore;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="dataStore"></param>
    public DiskStateRegistry(ILogger<DiskStateRegistry> logger, IDataStore dataStore)
    {
        _logger = logger;
        _dataStore = dataStore;
    }

    /// <summary>
    /// Saves a disk state to the data store
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="diskState"></param>
    /// <returns></returns>
    public void SaveState(LoadoutId loadoutId, DiskState diskState)
    {
        var iid = loadoutId.ToEntityId(EntityCategory.DiskState);
        using var ms = new MemoryStream();
        {
            using var compressed = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true);
            JsonSerializer.Serialize(compressed, diskState);
        }
        _dataStore.PutRaw(iid, ms.GetBuffer().AsSpan().SliceFast(0, (int)ms.Length));
    }

    /// <summary>
    /// Gets the disk state associated with a specific version of a loadout (if any)
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public DiskState? GetState(LoadoutId loadoutId)
    {
        var iid = loadoutId.ToEntityId(EntityCategory.DiskState);
        var data = _dataStore.GetRaw(iid);
        if (data == null) return null;
        using var ms = new MemoryStream(data);
        using var compressed = new GZipStream(ms, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<DiskState>(compressed);
    }
}
