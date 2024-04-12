using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.IngestOnly;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: FileTreeToFlattenedLoadout", 
    "[Ingest 6/9] Converts a file tree into a flattened loadout by assigning files to mods, " +
    "reusing previous assignments when possible, and creating new mods for unassigned files.")]
// Needed because DB keeps growing between runs, and DB perf can be inconsistent enough that it'll run all 100 runs,
// taking forever.
public class FileTreeToFlattenedLoadout : ASynchronizerBenchmark, IBenchmark
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
    private FileTree _fileTree = null!;
    
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
            _fileTree = await _defaultSynchronizer.DiskToFileTree(_diskState, loadout, _prevFileTree, _prevDiskState);
        }).Wait();
    }

    [Benchmark]
    public async Task<FlattenedLoadout> FileTreeToFlattenedLoadouto()
    {
        return await _defaultSynchronizer.FileTreeToFlattenedLoadout(_fileTree, _datamodel.BaseList.Value, _prevFlattenedLoadout);
    }
}
