using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Settings;
using NexusMods.App.GarbageCollection.DataModel;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.DataModel;

/// <inheritdoc />
public class GarbageCollectorRunner(ISettingsManager settings, NxFileStore store, IConnection connection) : IGarbageCollectorRunner
{
    private readonly DataModelSettings _settings = settings.Get<DataModelSettings>();
    private readonly NxFileStore _store = store;
    private readonly IConnection _connection = connection;

    /// <inheritdoc />
    public void Run()
    {
        RunGarbageCollector.Do(_settings.ArchiveLocations, _store, _connection);
    }
}
