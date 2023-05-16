using System.Collections.Immutable;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests.Harness;

public class ALoadoutSynrconizerTest<T> : ADataModelTest<T>
{
    protected readonly TestDirectoryIndexer TestIndexer;
    protected readonly LoadoutSyncronizer TestSyncronizer;
    protected readonly TestArchiveManager TestArchiveManagerInstance;
    protected readonly TestFingerprintCache<Mod, CachedModSortRules> TestFingerprintCacheInstance;
    protected readonly TestFingerprintCache<IGeneratedFile, CachedGeneratedFileData> TestGeneratedFileFingerprintCache;

    public ALoadoutSynrconizerTest(IServiceProvider provider) : base(provider)
    {
        TestIndexer = new TestDirectoryIndexer();
        TestArchiveManagerInstance = new TestArchiveManager();
        TestFingerprintCacheInstance = new TestFingerprintCache<Mod, CachedModSortRules>();
        TestGeneratedFileFingerprintCache = new TestFingerprintCache<IGeneratedFile, CachedGeneratedFileData>();
        TestSyncronizer = new LoadoutSyncronizer(TestFingerprintCacheInstance, TestIndexer, TestArchiveManagerInstance, TestGeneratedFileFingerprintCache);
    }



    protected class TestArchiveManager : IArchiveManager
    {
        public readonly HashSet<Hash> Archives = new();
        public async ValueTask<bool> HaveFile(Hash hash)
        {
            return Archives.Contains(hash);
        }
    }

    protected class TestDirectoryIndexer : IDirectoryIndexer
    {
        public List<HashedEntry> Entries = new();

        public async IAsyncEnumerable<HashedEntry> IndexFolders(IEnumerable<AbsolutePath> paths,
            CancellationToken token = default)
        {
            foreach (var entry in Entries)
            {
                yield return entry;
            }
        }
    }

    protected async Task<Loadout> CreateApplyPlanTestLoadout(bool generatedFile = false)
    {
        var loadout = await LoadoutManager.ManageGameAsync(Install, Guid.NewGuid().ToString());
        
        var mainMod = loadout.Value.Mods.Values.First();
        var files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore);
        if (generatedFile)
        {
            files = files.With(new TestGeneratedFile
            {
                Id = ModFileId.New(),
                To = new GamePath(GameFolderType.Game, "0x00001.generated"),
            }, m => m.Id);
        }
        else
        {
            files = files.With(new FromArchive
            {
                Id = ModFileId.New(),
                Hash = Hash.From(0x00001),
                Size = Size.From(0x10001),
                To = new GamePath(GameFolderType.Game, "0x00001.dat"),
            }, m => m.Id);
        }

        var mod = new Mod
        {
            Id = ModId.New(),
            Name = "Test Mod",
            Files = files,
            SortRules = ImmutableList<ISortRule<Mod, ModId>>.Empty
        };
        
        loadout.Add(mod);
        loadout.Remove(mainMod);
        return loadout.Value;
    }
    
    /// <summary>
    /// Create a test loadout with a number of mods each with a alphabetical sort rule
    /// </summary>
    /// <param name="numberMainFiles"></param>
    /// <returns></returns>
    protected async Task<LoadoutMarker> CreateTestLoadout(int numberMainFiles = 10)
    {
        var loadout = await LoadoutManager.ManageGameAsync(Install, Guid.NewGuid().ToString());

        var mainMod = loadout.Value.Mods.Values.First();

        var mods = Enumerable.Range(0,  numberMainFiles).Select(x => new Mod()
        {
            Id = ModId.New(),
            Name = $"Mod {x}",
            Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore),
            SortRules = new ISortRule<Mod, ModId>[]
            {
                new AlphabeticalSort()
            }.ToImmutableList()
        }).ToList();

        foreach (var mod in mods)
            loadout.Add(mod);

        loadout.Remove(mainMod);
        return loadout;
    }
    


    public class TestFingerprintCache<TSrc, TValue> : IFingerprintCache<TSrc, TValue> where TValue : Entity
    {
        public readonly Dictionary<Hash, TValue> Dict = new();
        public readonly Dictionary<Hash, int> GetCount = new();
        public readonly Dictionary<Hash, int> SetCount = new();

        public bool TryGet(Hash hash, out TValue value)
        {
            GetCount[hash] = GetCount.GetValueOrDefault(hash, 0) + 1;
            return Dict.TryGetValue(hash, out value);
        }

        public void Set(Hash hash, TValue value)
        {
            value.DataStoreId = new Id64(EntityCategory.Fingerprints, hash.Value);
            Dict[hash] = value;
            SetCount[hash] = SetCount.GetValueOrDefault(hash, 0) + 1;
        }
    }
}

[JsonName("TestGeneratedFile")]
public record TestGeneratedFile : AModFile, IGeneratedFile, IToFile, ITriggerFilter<(ModId, ModFileId), Loadout>
{
    public ITriggerFilter<(ModId, ModFileId), Loadout> TriggerFilter => this;
    public required GamePath To { get; init; }
    public Hash GetFingerprint((ModId, ModFileId) self, Loadout input)
    {
        var printer = Fingerprinter.Create();
        foreach (var mod in input.Mods)
            foreach (var file in mod.Value.Files)
                if (!file.Value.Id.Equals(self.Item2))
                    printer.Add(file.Value.DataStoreId);

        return printer.Digest();
    }
}


/// <summary>
/// Example generated sort rule that sorts all mods alphabetically
/// </summary>
[JsonName("TestGeneratedSortRule")]
public class AlphabeticalSort : IGeneratedSortRule, ISortRule<Mod, ModId>, ITriggerFilter<ModId, Loadout>
{
    public ITriggerFilter<ModId, Loadout> TriggerFilter => this;

    public async IAsyncEnumerable<ISortRule<Mod, ModId>> GenerateSortRules(ModId selfId, Loadout loadout)
    {
        var thisMod = loadout.Mods[selfId];
        foreach (var (modId, other) in loadout.Mods)
        {
            if (modId.Equals(selfId)) continue;
            if (string.Compare(other.Name, thisMod.Name, StringComparison.Ordinal) > 0)
            {
                yield return new Before<Mod, ModId>(other.Id);
            }
            else
            {
                yield return new After<Mod, ModId>(modId);
            }
        }
    }

    public Hash GetFingerprint(ModId self, Loadout input)
    {
        var fp = Fingerprinter.Create();
        fp.Add(input.Mods[self].DataStoreId);
        foreach (var name in input.Mods.Values.Select(n => n.Name).Order())
        {
            fp.Add(name);
        }
        return fp.Digest();
    }
}
