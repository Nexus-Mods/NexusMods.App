using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.IngestOnly;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: GetDiskState", 
    "[Ingest 4/9] Get the current state of the game on disk. " +
    "In this test, the entire previous state is cached, so this is more of an overhead test rather than actually indexing new data.")]
public class GetDiskState : ASynchronizerBenchmark, IBenchmark
{
    [ParamsSource(nameof(ValuesForFilePath))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string FileName = null!;
    private FlattenedLoadout _flattenedLoadout = null!;
    private FileTree _fileTree = null!;
    private DiskStateTree _prevState = null!;
    private GameInstallation _installation = null!;

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
        
        // To set this up, we first need to apply the files of a new loadout
        // (without updating the loadout itself). This way we have loose files.
        
        Task.Run(async () =>
        {
            // Do an apply, but without updating the loadout revision.
            _flattenedLoadout = await _defaultSynchronizer.LoadoutToFlattenedLoadout(_datamodel.BaseList.Value);
            _fileTree = await _defaultSynchronizer.FlattenedLoadoutToFileTree(_flattenedLoadout, _datamodel.BaseList.Value);
            _installation = _datamodel.Game.Installations.First();
            _prevState = _datamodel.DiskStateRegistry.GetState(_installation)!;
            await _defaultSynchronizer.FileTreeToDiskImpl(_fileTree, _datamodel.BaseList.Value, _flattenedLoadout, _prevState, _installation,false);
            var a = 5;
        }).Wait();
#pragma warning disable
        _flattenedLoadout = Task.Run(() => _defaultSynchronizer.LoadoutToFlattenedLoadout(_datamodel.BaseList.Value)).Result.Result;
    }

    [Benchmark]
    public async Task<DiskStateTree> GetState()
    {
        // Note: This benchmark has a 'cache' of previous index, so it's not a real-world scenario.
        return await _defaultSynchronizer.GetDiskState(_installation);
    }
}
