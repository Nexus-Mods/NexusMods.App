using FluentAssertions;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using IGeneratedFile = NexusMods.DataModel.LoadoutSynchronizer.IGeneratedFile;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class MakeApplyPlanTests : ALoadoutSynrchonizerTest<MakeApplyPlanTests>
{
    public MakeApplyPlanTests(IServiceProvider provider) : base(provider) { }


    #region Make Apply Plan Tests

    /// <summary>
    /// If a file doesn't exist, it should be created
    /// </summary>
    [Fact]
    public async Task FilesThatDontExistAreCreatedByPlan()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        var plan = await TestSyncronizer.MakeApplySteps(loadout);

        var fileOne = loadout.Mods.Values.First().Files.Values.OfType<IFromArchive>()
            .First(f => f.Hash == Hash.From(0x00001));

        plan.Steps.Should().ContainEquivalentOf(new ExtractFile
        {
            To = loadout.Installation.LocationsRegister[LocationId.Game].Combine("0x00001.dat"),
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


        var absPath = loadout.Installation.LocationsRegister[LocationId.Game].Combine("0x00001.dat");

        TestIndexer.Entries.Add(new HashedEntry(absPath, fileOne.Hash, DateTime.Now - TimeSpan.FromDays(1), fileOne.Size ));

        var plan = await TestSyncronizer.MakeApplySteps(loadout);

        plan.Steps.Should().NotContainEquivalentOf(new ExtractFile
        {
            To = loadout.Installation.LocationsRegister[LocationId.Game].Combine("0x00001.dat"),
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

        var absPath = loadout.Installation.LocationsRegister[LocationId.Game].Combine("file_to_delete.dat");
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x042), DateTime.Now - TimeSpan.FromDays(1), Size.From(0x33)));

        var plan = await TestSyncronizer.MakeApplySteps(loadout);

        plan.Steps.Should().ContainEquivalentOf(new DeleteFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x33)
        });

        plan.Steps.Should().ContainEquivalentOf(new BackupFile
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
        var absPath = loadout.Installation.LocationsRegister[LocationId.Game].Combine("file_to_delete.dat");
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x042), DateTime.Now - TimeSpan.FromDays(1),
            Size.From(0x33)));

        var plan = await TestSyncronizer.MakeApplySteps(loadout);

        plan.Steps.OfType<BackupFile>().Should().BeEmpty();
    }

    /// <summary>
    /// If a file in the plan differs from the one on disk, then back up the on-disk file, delete it
    /// then create the new file
    /// </summary>
    [Fact]
    public async Task ChangedFilesAreBackedUpDeletedAndCreated()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        var fileOne = loadout.Mods.Values.First(mod => mod.Enabled == true).Files.Values.OfType<IFromArchive>()
            .First(f => f.Hash == Hash.From(0x00001));

        var absPath = loadout.Installation.LocationsRegister[LocationId.Game].Combine("0x00001.dat");
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x042), DateTime.Now - TimeSpan.FromDays(1),
            Size.From(0x33)));

        var plan = await TestSyncronizer.MakeApplySteps(loadout);



        plan.Steps.Should().ContainEquivalentOf(new DeleteFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x33)
        });

        plan.Steps.Should().ContainEquivalentOf(new BackupFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x33)
        });

        plan.Steps.Should().ContainEquivalentOf(new ExtractFile
        {
            To = loadout.Installation.LocationsRegister[LocationId.Game].Combine("0x00001.dat"),
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

        var fileOne = loadout.Mods.Values.First().Files.Values.OfType<Loadouts.ModFiles.IGeneratedFile>()
            .First();

        var absPath = loadout.Installation.LocationsRegister[LocationId.Game].Combine("0x00001.generated");

        var plan = await TestSyncronizer.MakeApplySteps(loadout);

        var generateFile = plan.Steps.OfType<GenerateFile>().FirstOrDefault();
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

        var absPath = loadout.Installation.LocationsRegister[LocationId.Game].Combine("0x00001.generated");

        var plan = await TestSyncronizer.MakeApplySteps(loadout);

        plan.Steps.Should().ContainEquivalentOf(new ExtractFile
        {
            To = absPath,
            Hash = Hash.From(0x42),
            Size = Size.From(0x43)
        });
    }

    #endregion

}
