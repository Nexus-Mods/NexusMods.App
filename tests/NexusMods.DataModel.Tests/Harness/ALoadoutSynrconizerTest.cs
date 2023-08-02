using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
#pragma warning disable CS1998

namespace NexusMods.DataModel.Tests.Harness;

public class ALoadoutSynrchonizerTest<T> : ADataModelTest<T>
{
    protected readonly TestDirectoryIndexer TestIndexer;
    protected readonly LoadoutSynchronizer TestSyncronizer;
    protected readonly TestArchiveManager TestArchiveManagerInstance;
    protected readonly TestFingerprintCache<Mod, CachedModSortRules> TestFingerprintCacheInstance;
    protected readonly TestFingerprintCache<IGeneratedFile, CachedGeneratedFileData> TestGeneratedFileFingerprintCache;

    public ALoadoutSynrchonizerTest(IServiceProvider provider) : base(provider)
    {
        AssertionOptions.AssertEquivalencyUsing(opt => opt.ComparingRecordsByValue());

        TestIndexer = new TestDirectoryIndexer();
        TestArchiveManagerInstance = new TestArchiveManager();
        TestFingerprintCacheInstance = new TestFingerprintCache<Mod, CachedModSortRules>();
        TestGeneratedFileFingerprintCache = new TestFingerprintCache<IGeneratedFile, CachedGeneratedFileData>();
        TestSyncronizer = new LoadoutSynchronizer(
            provider.GetRequiredService<ILogger<LoadoutSynchronizer>>(), 
            TestFingerprintCacheInstance, 
            TestIndexer, 
            TestArchiveManagerInstance, 
            TestGeneratedFileFingerprintCache,
            LoadoutManager.Registry);
    }



    /// <summary>
    /// Get the `To` location of the first file in the first mod that is IToFile
    /// </summary>
    /// <param name="loadout"></param>
    /// <returns></returns>
    protected static AbsolutePath GetFirstModFile(Loadout loadout)
    {
        var to = loadout.Mods.Values.First().Files.Values.OfType<IToFile>().First().To;
        return to.Combine(loadout.Installation.Locations[GameFolderType.Game]);
    }


    protected class TestArchiveManager : IArchiveManager
    {
        public readonly HashSet<Hash> Archives = new();
        public readonly Dictionary<Hash, IStreamFactory> Extracted = new();
        public async ValueTask<bool> HaveFile(Hash hash)
        {
            return Archives.Contains(hash);
        }

        public async Task BackupFiles(IEnumerable<(IStreamFactory, Hash, Size)> backups, CancellationToken token = default)
        {
            Archives.AddRange(backups.Select(b => b.Item2));
        }

        public async Task ExtractFiles(IEnumerable<(Hash Src, AbsolutePath Dest)> files, CancellationToken token = default)
        {
            foreach (var entry in files)
                Extracted[entry.Src] = new NativeFileStreamFactory(entry.Dest);
        }

        public Task<IDictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public async Task ExtractFiles(IEnumerable<(Hash Src, IStreamFactory Dest)> files, CancellationToken token = default)
        {
            foreach (var entry in files)
                Extracted[entry.Src] = entry.Dest;
        }
    }

    protected class TestDirectoryIndexer : IDirectoryIndexer
    {
        public List<HashedEntry> Entries = new();

#pragma warning disable CS1998
        public async IAsyncEnumerable<HashedEntry> IndexFolders(IEnumerable<AbsolutePath> paths,
#pragma warning restore CS1998
            [EnumeratorCancellation] CancellationToken token = default)
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
        var disabledFiles = EntityDictionary<ModFileId, AModFile>.Empty(DataStore);

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
            SortRules = ImmutableList<ISortRule<Mod, ModId>>.Empty,
            Enabled = true
        };
        
        disabledFiles = files.With(new FromArchive
        {
            Id = ModFileId.New(),
            Hash = Hash.From(0x00001),
            Size = Size.From(0x10001),
            To = new GamePath(GameFolderType.Game, "shouldNotExist/0x00001.dat"),
        }, m => m.Id);
        
        var disabledMod = new Mod
        {
            Id = ModId.New(),
            Name = "Disabled Test Mod",
            Files = disabledFiles,
            SortRules = ImmutableList<ISortRule<Mod, ModId>>.Empty,
            Enabled = false,
        };
        
        
        loadout.Add(mod);
        loadout.Add(disabledMod);
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
            }.ToImmutableList(),
            Enabled = true
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
            return Dict.TryGetValue(hash, out value!);
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
public record TestGeneratedFile : AModFile, IGeneratedFile, IToFile, ITriggerFilter<ModFilePair, Plan>
{
    public ITriggerFilter<ModFilePair, Plan> TriggerFilter => this;
    public Task<Hash> GenerateAsync(Stream stream, ApplyPlan loadout, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public required GamePath To { get; init; }
    public Hash GetFingerprint(ModFilePair self, Plan plan)
    {
        using var printer = Fingerprinter.Create();
        foreach (var mod in plan.Loadout.Mods)
        {
            if (mod.Value.Enabled == false) 
                continue;
            
            foreach (var file in mod.Value.Files)
                if (!file.Value.Id.Equals(self.File.Id))
                    printer.Add(file.Value.DataStoreId);
        }

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

    public Hash GetFingerprint(ModId self, Loadout loadout)
    {
        using var fp = Fingerprinter.Create();
        fp.Add(loadout.Mods[self].DataStoreId);
        foreach (var name in loadout.Mods.Values.Select(n => n.Name).Order())
        {
            fp.Add(name);
        }
        return fp.Digest();
    }


}
