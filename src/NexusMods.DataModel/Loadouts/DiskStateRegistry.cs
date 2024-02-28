using System.Diagnostics;
using System.IO.Compression;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using Reloaded.Memory.Extensions;

namespace NexusMods.DataModel.Loadouts;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public class DiskStateRegistry : IDiskStateRegistry
{
    private readonly ILogger<DiskStateRegistry> _logger;
    private readonly IDataStore _dataStore;
    private readonly IDictionary<GameInstallation, IId> _lastAppliedRevisionDictionary = new Dictionary<GameInstallation, IId>();
    private readonly Subject<(GameInstallation gameInstallation, IId loadoutRevision)> _lastAppliedRevisionSubject = new();

    /// <inheritdoc />
    public IObservable<(GameInstallation gameInstallation, IId loadoutRevision)> LastAppliedRevisionObservable => _lastAppliedRevisionSubject;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public DiskStateRegistry(ILogger<DiskStateRegistry> logger, IDataStore dataStore)
    {
        _logger = logger;
        _dataStore = dataStore;
    }

    /// <summary>
    /// Saves a disk state to the data store
    /// </summary>
    /// <returns></returns>
    public void SaveState(GameInstallation installation, DiskStateTree diskState)
    {
        Debug.Assert(!diskState.LoadoutRevision.Equals(IdEmpty.Empty), "diskState.LoadoutRevision must be set");
        var iid = MakeId(installation);
        using var ms = new MemoryStream();
        {
            using var compressed = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true);
            JsonSerializer.Serialize(compressed, diskState);
        }
        _dataStore.PutRaw(iid, ms.GetBuffer().AsSpan().SliceFast(0, (int)ms.Length));
        // TODO: this might need to be made thread safe
        _lastAppliedRevisionDictionary[installation] = diskState.LoadoutRevision;
        _lastAppliedRevisionSubject.OnNext((installation, diskState.LoadoutRevision));
    }

    private IId MakeId(GameInstallation installation)
    {
        var str = $"{installation.Game.GetType()}|{installation.LocationsRegister[LocationId.Game]}";

        var bytes = Encoding.UTF8.GetBytes(str);
        return IId.FromSpan(EntityCategory.DiskState, bytes);
    }

    /// <summary>
    /// Gets the disk state associated with a specific version of a loadout (if any)
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns></returns>
    public DiskStateTree? GetState(GameInstallation gameInstallation)
    {
        var iid = MakeId(gameInstallation);
        var data = _dataStore.GetRaw(iid);
        if (data == null) return null;
        using var ms = new MemoryStream(data);
        using var compressed = new GZipStream(ms, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<DiskStateTree>(compressed);
    }

    /// <Inheritdoc />
    public IId? GetLastAppliedLoadout(GameInstallation gameInstallation)
    {
        if (_lastAppliedRevisionDictionary.TryGetValue(gameInstallation, out var lastAppliedLoadout))
        {
            return lastAppliedLoadout;
        }

        var diskStateTree = GetState(gameInstallation);
        if (diskStateTree is null) return null;
        Debug.Assert(!diskStateTree.LoadoutRevision.Equals(IdEmpty.Empty), "diskState.LoadoutRevision must be set");

        _lastAppliedRevisionDictionary[gameInstallation] = diskStateTree.LoadoutRevision;
        return diskStateTree.LoadoutRevision;
    }
}
