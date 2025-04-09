using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Settings;
using NexusMods.App.GarbageCollection.DataModel;
using NexusMods.CrossPlatform;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.DataModel;

/// <inheritdoc />
public class GarbageCollectorRunner(ISettingsManager settings, NxFileStore store, IConnection connection, ILogger<GarbageCollectorRunner> logger) : IGarbageCollectorRunner
{
    private readonly DataModelSettings _settings = settings.Get<DataModelSettings>();
    private readonly NxFileStore _store = store;
    private readonly IConnection _connection = connection;
    private readonly ILogger<GarbageCollectorRunner> _logger = logger;

    /// <inheritdoc />
    public void Run()
    {
        RunGarbageCollector.Do(_settings.ArchiveLocations, _store, _connection);
    }
    
    /// <inheritdoc />
    public Task RunAsync()
    {
        return Task.Run(Run);
    }
    
    /// <inheritdoc />
    public async Task RunWithMode(GarbageCollectorRunMode gcRunMode)
    {
        switch (gcRunMode)
        {
            case GarbageCollectorRunMode.RunSynchronously:
                Run();
                break;
            case GarbageCollectorRunMode.RunAsyncInBackground:
                RunAsync().FireAndForget(_logger);
                break;
            case GarbageCollectorRunMode.RunAsynchronously:
                await RunAsync();
                break;
            case GarbageCollectorRunMode.DoNotRun:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(gcRunMode), gcRunMode, null);
        }
    }
}
