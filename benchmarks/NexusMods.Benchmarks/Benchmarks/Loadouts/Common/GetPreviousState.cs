using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.Common;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: GetPreviousState", "[Apply Step 3/5, Ingest 3/9] Retrieves the serialized previous (expected) disk state.")]
public class GetPreviousState : ASynchronizerBenchmark, IBenchmark
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
        Init("Benchmark Mod Files", filePath);
        _flattenedLoadout = Task.Run<FlattenedLoadout>(async () => await _defaultSynchronizer.LoadoutToFlattenedLoadout(_datamodel.BaseLoadout)).Result;
    }

    [Benchmark]
    public DiskStateTree GetState()
    {
        return _datamodel.DiskStateRegistry.GetState(_installation)!;
    }
}
