using BenchmarkDotNet.Attributes;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Benchmarks.Benchmarks.Loadouts.Harness;
using NexusMods.Benchmarks.Interfaces;

namespace NexusMods.Benchmarks.Benchmarks.Loadouts.Common;

[MemoryDiagnoser]
[BenchmarkInfo("LoadoutSynchronizer: LoadoutToFlattenedLoadout", "[Apply Step 1/5, Ingest 1/9] Converts a loadout to a flattened loadout.")]
public class LoadoutToFlattenedLoadout : ASynchronizerBenchmark, IBenchmark
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
        Init("Benchmark Mod Files", Assets.Loadouts.FileLists.GetFileListPathByFileName(FileName));
    }

    [Benchmark]
    public async Task<FlattenedLoadout> FlattenLoadout()
    {
        return await _defaultSynchronizer.LoadoutToFlattenedLoadout(_datamodel.BaseList.Value);
    }
}
