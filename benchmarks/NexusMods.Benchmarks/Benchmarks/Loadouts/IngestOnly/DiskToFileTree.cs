using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.IngestOnly;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: DiskToFileTree", 
    "[Ingest 5/9] Create new file tree from the current disk state and the previous file tree.")]
public class DiskToFileTree : ASynchronizerBenchmark, IBenchmark
{
    [ParamsSource(nameof(ValuesForFilePath))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string FileName = null!;
    private FlattenedLoadout _prevFlattenedLoadout = null!;
    private FileTree _prevFileTree = null!;
    private DiskStateTree _prevDiskState = null!;
    private DiskStateTree _diskState = null!;
    
    public IEnumerable<string> ValuesForFilePath => new[]
    {
        Path.GetFileName(Assets.Loadouts.FileLists.SkyrimFileList),
        Path.GetFileName(Assets.Loadouts.FileLists.StardewValleyFileList),
        Path.GetFileName(Assets.Loadouts.FileLists.NPC3DFileList),
    };

    [GlobalSetup]
    public void Setup()
    {
        var filePath = Assets.Loadouts.FileLists.GetFileListPathByFileName(FileName);
        Init("Game Files", filePath);
        InitForIngest();
        var loadout = _datamodel.BaseList.Value;
        
        // Init for function.
        Task.Run(async () =>
        {
            _prevFlattenedLoadout = await _defaultSynchronizer.LoadoutToFlattenedLoadout(loadout);
            _prevFileTree = await _defaultSynchronizer.FlattenedLoadoutToFileTree(_prevFlattenedLoadout, loadout);
            _prevDiskState = _diskStateRegistry.GetState(_installation)!;

            // Get the new disk state
            _diskState = await _defaultSynchronizer.GetDiskState(_installation);
        }).Wait();
    }

    [Benchmark]
    public async Task<FileTree> GetState()
    {
        // Note: This benchmark has a 'cache' of previous index, so it's not a real-world scenario.
        return await _defaultSynchronizer.DiskToFileTree(_diskState, _datamodel.BaseList.Value, _prevFileTree, _prevDiskState);
    }
}
