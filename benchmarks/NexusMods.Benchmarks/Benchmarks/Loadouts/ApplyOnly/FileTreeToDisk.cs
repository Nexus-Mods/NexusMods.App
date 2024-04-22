using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.ApplyOnly;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: FileTreeToDisk", "[Apply Step 4/5] Compares expected state against new state and extracts files to target. (Extraction Skipped)")]
public class FileTreeToDisk : ASynchronizerBenchmark, IBenchmark
{
    [ParamsSource(nameof(ValuesForFilePath))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string FileName = null!;
    private FlattenedLoadout _flattenedLoadout = null!;
    private FileTree _fileTree = null!;
    private DiskStateTree _prevState = null!;
    
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
        Init("Benchmark Mod Files", filePath);
        Task.Run(async () =>
        {
            _flattenedLoadout = await _defaultSynchronizer.LoadoutToFlattenedLoadout(_datamodel.BaseList.Value);
            _fileTree = await _defaultSynchronizer.FlattenedLoadoutToFileTree(_flattenedLoadout, _datamodel.BaseList.Value);
            _prevState = _datamodel.DiskStateRegistry.GetState(_installation)!;
        }).Wait();
#pragma warning disable CS0618 // Type or member is obsolete
        _defaultSynchronizer.SetFileStore(new DummyFileStore());
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Benchmark]
    public async Task<DiskStateTree> ToDisk()
    {
        return await _defaultSynchronizer.FileTreeToDiskImpl
            (_fileTree, _datamodel.BaseList.Value, _flattenedLoadout, _prevState, _installation,false);
    }
}
