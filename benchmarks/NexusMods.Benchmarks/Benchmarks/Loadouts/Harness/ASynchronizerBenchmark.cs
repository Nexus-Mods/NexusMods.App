using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.Loadouts;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;

/// <summary>
///     Helper for running synchronizer related benchmarks.
///     Gives us a fake synchronizer with specified mods/files.
/// </summary>
public class ASynchronizerBenchmark
{
    protected ABenchmarkDatamodel _datamodel;
    protected IServiceProvider _serviceProvider;
    protected DefaultSynchronizer _defaultSynchronizer;
    
    public void Init(string baseModName, string fileList)
    {
        // Initialize Test Harness
        var services = new ServiceCollection();
        DataModel.Tests.Startup.ConfigureTestedServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        // Create a DataModel for Benchmarking
        var files = Assets.Loadouts.FileLists.GetFileList(fileList);
        _datamodel = ABenchmarkDatamodel.WithMod(_serviceProvider, baseModName, files);
        _defaultSynchronizer = (_datamodel.Game.Synchronizer as DefaultSynchronizer)!;
        if (_defaultSynchronizer == null)
            throw new Exception($"Can't cast synchronizer to {typeof(DefaultSynchronizer)}. Did the test StubbedGame code change?");
    }
}
