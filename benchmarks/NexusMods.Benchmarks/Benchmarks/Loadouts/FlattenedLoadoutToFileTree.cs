using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: FlattenedLoadoutToFileTree", "[Apply Step 2/5] Converts a flattened loadout to a file tree.")]
public class FlattenedLoadoutToFileTree : ASynchronizerBenchmark, IBenchmark
{
    [ParamsSource(nameof(ValuesForFilePath))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string FileName = null!;
    private FlattenedLoadout _flattenedLoadout = null!;

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
        _flattenedLoadout = Task.Run(() => _defaultSynchronizer.LoadoutToFlattenedLoadout(_datamodel.BaseList.Value)).Result.Result;
    }

    [Benchmark]
    public async Task<FileTree> ToFileTree()
    {
        return await _defaultSynchronizer.FlattenedLoadoutToFileTree(_flattenedLoadout, _datamodel.BaseList.Value);
    }
}
