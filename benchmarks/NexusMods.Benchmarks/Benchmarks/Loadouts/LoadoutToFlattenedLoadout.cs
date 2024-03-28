using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: LoadoutToFlattenedLoadout", "Converts a loadout to a flattened loadout. (First Step of Apply/Ingest Process)")]
public class LoadoutToFlattenedLoadout : ASynchronizerBenchmark, IBenchmark
{
    [ParamsSource(nameof(ValuesForFilePath))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    public string FilePath = null!;

    public IEnumerable<string> ValuesForFilePath => new[]
    {
        Assets.Loadouts.FileLists.SkyrimFileList,
        Assets.Loadouts.FileLists.StardewValleyFileList,
        Assets.Loadouts.FileLists.NPC3DFileList,
    };

    [GlobalSetup]
    public void Setup()
    {
        Init("Game Files", FilePath);
    }

    [Benchmark]
    public async Task<FlattenedLoadout> FlattenLoadout()
    {
        return await _defaultSynchronizer.LoadoutToFlattenedLoadout(_datamodel.BaseList.Value);
    }
}
