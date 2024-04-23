using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.DiskState;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.IngestOnly;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: GetDiskState (With Cache)", 
    "[Ingest 4/9] Get the current state of the game on disk. " +
    "In this test, the entire previous state is cached, so this is more of an overhead test rather than actually indexing new data.")]
public class GetDiskState : ASynchronizerBenchmark, IBenchmark
{
    [ParamsSource(nameof(ValuesForFilePath))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string FileName = null!;

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
        InitForIngest();
    }

    [Benchmark]
    public async Task<DiskStateTree> GetCurrentDiskState_WithCachedHashes()
    {
        // Note: This benchmark has a 'cache' of previous index, so it's not a real-world scenario.
        // So this is purely an overhead test.
        return await _defaultSynchronizer.GetDiskState(_installation);
    }
}
