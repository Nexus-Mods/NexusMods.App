using System.Collections.Immutable;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class LoadoutSyncronizerTests : ALoadoutSynrconizerTest<LoadoutSyncronizerTests>
{
    public LoadoutSyncronizerTests(IServiceProvider provider) : base(provider)
    {

    }
    
    [Fact]
    public async Task GeneratedSortRulesAreFetched()
    {
       var loadout = await CreateTestLoadout(4);

       var mod = loadout.Value.Mods.Values.First(m => m.Name == "Mod 2");

       var nameForId = loadout.Value.Mods.Values.ToDictionary(m => m.Id, m => m.Name);

       var rules = await LoadoutSyncronizer.ModSortRules(loadout.Value, mod).ToListAsync();


       var testData = rules.Select(r =>
       {
           if (r is After<Mod, ModId> a) return ("After", nameForId[a.Other]);
           if (r is Before<Mod, ModId> b) return ("Before", nameForId[b.Other]);
           throw new NotImplementedException();
       });

       testData.Should().BeEquivalentTo(new[]
       {
           ("After", "Mod 0"),
           ("After", "Mod 1"),
           ("Before", "Mod 3")
       });
    }

    [Fact]
    public async Task CanSortMods()
    {
        var loadout = await CreateTestLoadout();

        var mods = await LoadoutSyncronizer.SortMods(loadout.Value);
        mods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "Mod 0",
            "Mod 1",
            "Mod 2",
            "Mod 3",
            "Mod 4",
            "Mod 5",
            "Mod 6",
            "Mod 7",
            "Mod 8",
            "Mod 9"
        }, opt => opt.WithStrictOrdering());
    }

    [Fact]
    public async Task StaticRulesAreConsidered()
    {
        var lastMod = new Mod()
        {
            Id = ModId.New(),
            Name = "zz Last Mod",
            Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore),
            SortRules = new ISortRule<Mod, ModId>[]
            {
                new First<Mod, ModId>()
            }.ToImmutableList()
        };

        var loadout = await CreateTestLoadout();
        loadout.Add(lastMod);

        await LoadoutSyncronizer.Invoking(_ => LoadoutSyncronizer.SortMods(loadout.Value))
            .Should().ThrowAsync<InvalidOperationException>("rule conflicts with generated rules");
    }

    [Fact]
    public async Task SortRulesAreCached()
    {
        var loadout = await CreateTestLoadout(2);

        await TestSyncronizer.SortMods(loadout.Value);

        TestFingerprintCacheInstance.Dict.Should().HaveCount(2);

        TestFingerprintCacheInstance.GetCount.Values.Should().AllBeEquivalentTo(1);
        TestFingerprintCacheInstance.SetCount.Values.Should().AllBeEquivalentTo(1);
        
        await TestSyncronizer.SortMods(loadout.Value);
        
        TestFingerprintCacheInstance.GetCount.Values.Should().AllBeEquivalentTo(2);
        TestFingerprintCacheInstance.SetCount.Values.Should().AllBeEquivalentTo(1);

    }

    #region Make Apply Plan Tests

    /// <summary>
    /// If a file doesn't exist, it should be created 
    /// </summary>
    [Fact]
    public async Task FilesThatDontExistAreCreatedByPlan()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        
        var plan = await TestSyncronizer.MakeApplySteps(loadout).ToListAsync();

        var fileOne = loadout.Mods.Values.First().Files.Values.OfType<IFromArchive>()
            .First(f => f.Hash == Hash.From(0x00001));

        plan.Should().ContainEquivalentOf(new ExtractFile
        {
            To = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat"),
            Hash = fileOne.Hash,
            Size = fileOne.Size
        });
    }
    
    /// <summary>
    /// Files that are already in the correct state in the game folder shouldn't be re-extracted
    /// </summary>
    [Fact]
    public async Task FilesThatExistAreNotCreatedByPlan()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        
        var fileOne = loadout.Mods.Values.First().Files.Values.OfType<IFromArchive>()
            .First(f => f.Hash == Hash.From(0x00001));


        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat");

        TestIndexer.Entries.Add(new HashedEntry(absPath, fileOne.Hash, DateTime.Now - TimeSpan.FromDays(1), fileOne.Size ));
        
        var plan = await TestSyncronizer.MakeApplySteps(loadout).ToListAsync();
        
        plan.Should().NotContainEquivalentOf(new ExtractFile
        {
            To = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat"),
            Hash = fileOne.Hash,
            Size = fileOne.Size
        });
    }
    
    /// <summary>
    /// Files that are in the game folders, but not in the plan should be backed up then deleted
    /// </summary>
    [Fact]
    public async Task ExtraFilesAreDeletedAndBackedUp()
    { 
        var loadout = await CreateApplyPlanTestLoadout();
        
        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("file_to_delete.dat");
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x042), DateTime.Now - TimeSpan.FromDays(1), Size.From(0x33)));

        var plan = await TestSyncronizer.MakeApplySteps(loadout).ToListAsync();

        plan.Should().ContainEquivalentOf(new DeleteFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x33)
        });

        plan.Should().ContainEquivalentOf(new BackupFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x33)
        });
    }

    /// <summary>
    /// If a file is backed up, we shouldn't see a command to back it up again. 
    /// </summary>
    [Fact]
    public async Task FilesAreNotBackedUpIfAlreadyBackedUp()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        TestArchiveManagerInstance.Archives.Add(Hash.From(0x042));
        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("file_to_delete.dat");
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x042), DateTime.Now - TimeSpan.FromDays(1),
            Size.From(0x33)));

        var plan = await TestSyncronizer.MakeApplySteps(loadout).ToListAsync();

        plan.OfType<BackupFile>().Should().BeEmpty();
    }
    
    /// <summary>
    /// If a file in the plan differs from the one on disk, then back up the on-disk file, delete it
    /// then create the new file
    /// </summary>
    [Fact]
    public async Task ChangedFilesAreBackedUpDeletedAndCreated()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        var fileOne = loadout.Mods.Values.First().Files.Values.OfType<IFromArchive>()
            .First(f => f.Hash == Hash.From(0x00001));

        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat");
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x042), DateTime.Now - TimeSpan.FromDays(1),
            Size.From(0x33)));

        var plan = await TestSyncronizer.MakeApplySteps(loadout).ToListAsync();

        plan.Should().ContainEquivalentOf(new DeleteFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x33)
        });

        plan.Should().ContainEquivalentOf(new BackupFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x33)
        });
        
        plan.Should().ContainEquivalentOf(new ExtractFile
        {
            To = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat"),
            Hash = fileOne.Hash,
            Size = fileOne.Size
        });
    }
    
    /// <summary>
    /// Generated files that have never been generated before should be generated
    /// </summary>
    [Fact]
    public async Task GeneratedFilesAreCreated()
    {
        var loadout = await CreateApplyPlanTestLoadout(generatedFile: true);

        var fileOne = loadout.Mods.Values.First().Files.Values.OfType<IGeneratedFile>()
            .First();

        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.generated");
        
        var plan = await TestSyncronizer.MakeApplySteps(loadout).ToListAsync();

        var generateFile = plan.OfType<GenerateFile>().FirstOrDefault();
        generateFile.Should().NotBeNull();

        generateFile!.Source.Should().Be(fileOne);
        generateFile!.To.Should().BeEquivalentTo(absPath);
        generateFile.Fingerprint.Should().Be(Hash.From(17241709254077376921));
    }

    /// <summary>
    /// If a generated file would create data that is already backed up and archived, then extract it instead
    /// of regenerating the file
    /// </summary>
    [Fact]
    public async Task GeneratedFilesFromArchivesAreExtracted()
    {
        var loadout = await CreateApplyPlanTestLoadout(generatedFile: true);

        TestGeneratedFileFingerprintCache.Dict.Add(Hash.From(17241709254077376921), new CachedGeneratedFileData
        {
            Hash = Hash.From(0x42),
            Size = Size.From(0x43)
        });

        TestArchiveManagerInstance.Archives.Add(Hash.From(0x42));

        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.generated");
        
        var plan = await TestSyncronizer.MakeApplySteps(loadout).ToListAsync();

        plan.Should().ContainEquivalentOf(new ExtractFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x43)
        });
    }
    
    #endregion

}
