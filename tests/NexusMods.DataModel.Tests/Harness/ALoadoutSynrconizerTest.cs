using FluentAssertions;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Abstractions.Triggers;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

#pragma warning disable CS1998

namespace NexusMods.DataModel.Tests.Harness;

public class ALoadoutSynrchonizerTest<T> : ADataModelTest<T>
{
    protected readonly TestFileStore TestFileStoreInstance;
    protected readonly TestFingerprintCache<Mod.Model, CachedModSortRules> TestFingerprintCacheInstance;

    public ALoadoutSynrchonizerTest(IServiceProvider provider) : base(provider)
    {
        AssertionOptions.AssertEquivalencyUsing(opt => opt.ComparingRecordsByValue());

        TestFileStoreInstance = new TestFileStore();
        TestFingerprintCacheInstance = new TestFingerprintCache<Mod.Model, CachedModSortRules>();
    }
    
    protected class TestFileStore : IFileStore
    {
        public readonly HashSet<Hash> Archives = new();
        public readonly Dictionary<Hash, IStreamFactory> Extracted = new();
        public async ValueTask<bool> HaveFile(Hash hash)
        {
            return Archives.Contains(hash);
        }
        public async Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, CancellationToken token = default)
        {
            Archives.AddRange(backups.Select(b => b.Hash));
        }

        public async Task ExtractFiles((Hash Hash, AbsolutePath Dest)[] files, CancellationToken token = default)
        {
            foreach (var entry in files)
                Extracted[entry.Hash] = new NativeFileStreamFactory(entry.Dest);
        }

        public Task<Dictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
        {
            throw new NotSupportedException();
        }

        public Task<Stream> GetFileStream(Hash hash, CancellationToken token = default)
        {
            throw new NotSupportedException();
        }

        public HashSet<ulong> GetFileHashes()
        {
            throw new NotSupportedException();
        }

        public async Task ExtractFiles(IEnumerable<(Hash Src, IStreamFactory Dest)> files, CancellationToken token = default)
        {
            foreach (var entry in files)
                Extracted[entry.Src] = entry.Dest;
        }
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
