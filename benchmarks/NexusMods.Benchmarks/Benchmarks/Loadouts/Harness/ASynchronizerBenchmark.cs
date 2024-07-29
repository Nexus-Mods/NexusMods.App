using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;

/// <summary>
///     Helper for running synchronizer related benchmarks.
///     Gives us a fake synchronizer with specified mods/files.
/// </summary>
public class ASynchronizerBenchmark
{
    protected ABenchmarkDatamodel _datamodel = null!;
    protected IServiceProvider _serviceProvider = null!;
    protected DefaultSynchronizer DefaultSynchronizer = null!;
    protected GameInstallation _installation = null!;
    protected IDiskStateRegistry _diskStateRegistry = null!;

    protected void Init(string baseModName, string fileList)
    {
        // Initialize Test Harness
        var services = new ServiceCollection();
        DataModel.Tests.Startup.ConfigureTestedServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        // Create a DataModel for Benchmarking
        var files = Assets.Loadouts.FileLists.GetFileList(fileList);
        _datamodel = ABenchmarkDatamodel.WithMod(_serviceProvider, baseModName, files);
        DefaultSynchronizer = (_datamodel.Game.Synchronizer as DefaultSynchronizer)!;
        if (DefaultSynchronizer == null)
            throw new Exception($"Can't cast synchronizer to {typeof(DefaultSynchronizer)}. Did the test StubbedGame code change?");

        _installation = _datamodel.BaseLoadout.InstallationInstance;
        _diskStateRegistry = _serviceProvider.GetRequiredService<IDiskStateRegistry>();
    }
}
