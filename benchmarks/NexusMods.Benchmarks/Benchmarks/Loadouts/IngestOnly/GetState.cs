using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.DiskState;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.IngestOnly;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: GetExpectedDiskState", 
    "[Ingest 3/9] Get the serialized expected state of the game on disk.")]
public class GetState : ASynchronizerBenchmark, IBenchmark
{
    [ParamsSource(nameof(ValuesForFilePath))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string FileName = null!;

    public IEnumerable<string> ValuesForFilePath => new[]
    {
        Path.GetFileName(Assets.Loadouts.FileLists.SkyrimFileList),
    };

    [GlobalSetup]
    public void Setup()
    {
        var filePath = Assets.Loadouts.FileLists.GetFileListPathByFileName(FileName);
        Init("Game Files", filePath);
        InitForIngest();
    }

    [Benchmark]
    public DiskStateTree GetCachedDiskState()
    {
        return _diskStateRegistry.GetState(_installation)!;
    }
}
