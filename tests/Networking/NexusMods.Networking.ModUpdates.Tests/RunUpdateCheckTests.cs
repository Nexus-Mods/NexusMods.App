using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.NexusWebApi;
using Xunit.Abstractions;
using FileId = NexusMods.Abstractions.NexusWebApi.Types.V2.FileId;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.V2.ModId;

namespace NexusMods.Networking.ModUpdates.Tests;

public class RunUpdateCheckTests : ACyberpunkIsolatedGameTest<RunUpdateCheckTests>
{
    private readonly IGameDomainToGameIdMappingCache _mappingCache;
    private readonly NexusModsLibrary _nexusModsLibrary;

    public RunUpdateCheckTests(ITestOutputHelper outputHelper, IGameDomainToGameIdMappingCache mappingCache) : base(outputHelper)
    {
        _mappingCache = mappingCache;
        _nexusModsLibrary = ServiceProvider.GetRequiredService<NexusModsLibrary>();
    }

    // TODO: Add more tests in here. - Sewer
    // We shouldn't be relying on live site data for testing, however, we are still in
    // the process of getting the remaining V2 APIs in. In order to avoid wasted effort,
    // we won't be mocking the V1 APIs; so more complex/stable tests involving mocks will
    // move after full V2 move.

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task UpdatingModPageMetadata_ViaWebApi_ShouldWork()
    {
        // Create loadout
        var loadout = await CreateLoadout();
        
        // Install a version of CET into the loadout.
        // Name: 'CET 1.23.0'
        var modId = ModId.From(107u); // CET
        var fileId = FileId.From(39639u); // 1.18.1

        await using var tempFile = TemporaryFileManager.CreateFile();
        var downloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempFile,
            Cyberpunk2077Game.GameIdStatic,
            modId: modId,
            fileId: fileId
        );

        // install to loadout
        var libraryFile = await LibraryService.AddDownload(downloadJob);
        await LibraryService.InstallItem(libraryFile.AsLibraryItem(), loadout);
        
        // Ensure we're actually doing work
        var updates = await RunUpdateCheck.CheckForModPagesWhichNeedUpdating(Connection.Db, NexusNexusApiClient, _mappingCache);
        
        // We're relying on real data (CET), not a placeholder page.
        // Creating a placeholder is against TOS/Guidelines, so for now we
        // can only assert some 'general' knowledge.
        // A single mod page got updated here.
        updates.OutOfDateItems.Should().HaveCount(1);
        var outOfDateMod = NexusModsModPageMetadata.FindByUid(Connection.Db, updates.OutOfDateItems.First().GetModPageId()).First();
        var outOfDateFileUid = outOfDateMod.Files.First().Uid;

        // Fetch updated content for mod pages.
        using var tx = Connection.BeginTransaction();
        var gqlClient = ServiceProvider.GetRequiredService<NexusGraphQLClient>();
        await RunUpdateCheck.UpdateModFilesForOutdatedPages(Connection.Db, tx, Logger, gqlClient, updates, CancellationToken.None);
        await tx.Commit();

        // Get the collection of newer mods, there should at least be 43 at time of
        // collection. We're not filtering out archived, so this number can never change
        var newerMods = RunUpdateCheck.GetNewerFilesForExistingFile(Connection.Db, outOfDateFileUid);
        newerMods.Should().HaveCountGreaterThan(7);
        // Note: >=7 at time of writing.
        // this is just a sanity test, there's a separate test against mocked
        // data for the file picking logic
    }
}

/// <summary>
/// Tests specific to selecting newer files for an existing files.
/// This logic is part of <see cref="RunUpdateCheck"/> but placed in separate
/// class for easier segregation.
/// </summary>
public class GetNewerFilesSelectorTests
{
    private static DateTimeOffset BaseTime => new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldReturnEmpty_WhenNoOtherFiles()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("TestMod.zip", "1.0", BaseTime);
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[0]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldReturnEmpty_WhenOnlyOlderFiles()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("OldFile 1.0", "1.0", BaseTime.AddDays(-1));
            page.AddFile("OldFile 2.0", "2.0", BaseTime);
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[1]);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldReturnNewerFiles_WhenMatchingNamesExist()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("TestMod.zip", "1.0", BaseTime);
            page.AddFile("TestMod.zip", "2.0", BaseTime.AddDays(1));
            page.AddFile("TestMod.zip", "3.0", BaseTime.AddDays(2));
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[0]).ToArray();

        result.Should().HaveCount(2);
        result.Select(f => f.Version).Should().BeEquivalentTo("2.0", "3.0");
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldIgnoreDifferentNames()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("TestMod.zip", "1.0", BaseTime);
            page.AddFile("DifferentMod.zip", "2.0", BaseTime.AddDays(1));
            page.AddFile("TestMod.zip", "3.0", BaseTime.AddDays(2));
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[0]).ToArray();

        result.Should().HaveCount(1);
        result.Single().Version.Should().Be("3.0");
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldHandleVersionsInNames()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("TestMod 1.0.zip", "1.0", BaseTime);
            page.AddFile("TestMod 2.0.zip", "2.0", BaseTime.AddDays(1));
            page.AddFile("TestMod 3.0.zip", "3.0", BaseTime.AddDays(2));
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[0]).ToArray();

        result.Should().HaveCount(2);
        result.Select(f => f.Version).Should().BeEquivalentTo("2.0", "3.0");
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldHandleDifferentExtensions()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("TestMod.zip", "1.0", BaseTime);
            page.AddFile("TestMod.7z", "2.0", BaseTime.AddDays(1));
            page.AddFile("TestMod.rar", "3.0", BaseTime.AddDays(2));
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[0]).ToArray();

        result.Should().HaveCount(2);
        result.Select(f => f.Version).Should().BeEquivalentTo("2.0", "3.0");
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldHandleUnderscoresAndSpaces()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("Test_Mod.zip", "1.0", BaseTime);
            page.AddFile("Test Mod.zip", "2.0", BaseTime.AddDays(1));
            page.AddFile("Test_Mod.zip", "3.0", BaseTime.AddDays(2));
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[0]).ToArray();

        result.Should().HaveCount(2);
        result.Select(f => f.Version).Should().BeEquivalentTo("2.0", "3.0");
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldHandleVersionPrefixes()
    {
        var modPage = MockModPage.Create(page =>
        {
            page.AddFile("TestMod.zip", "v1.0", BaseTime);
            page.AddFile("TestMod.zip", "v2.0", BaseTime.AddDays(1));
            page.AddFile("TestMod.zip", "2.0", BaseTime.AddDays(2));  // No prefix
        });

        var result = RunUpdateCheck.GetNewerFilesForExistingFile(modPage.Files[0]).ToArray();

        // Note(sewer): This tests that our code can match with either.
        // The fact it returns both 'v2.0' and '2.0' here is not assumed to be an error,
        // as it is unlikely a mod author would publish a mod ***with the same name***
        // and ***version*** with and without a prefix.
        result.Should().HaveCount(2);
        result.Select(f => f.Version).Should().BeEquivalentTo("v2.0", "2.0");
    }

    [Fact]
    public void GetNewerFilesForExistingFile_ShouldHandleComplexExample()
    {
        var modPage = MockModPage.Create(page =>
        {
            // Different parts with same version
            page.AddFile("Skyrim 202X 9.0 - Architecture PART 1", "9.0", BaseTime);
            page.AddFile("Skyrim 202X 9.0 - Landscape PART 2", "9.0", BaseTime);
            
            // Updates to Part 1
            page.AddFile("Skyrim 202X 10.0 - Architecture PART 1", "10.0", BaseTime.AddDays(1));
            page.AddFile("Skyrim_202X_10.0.1 - Architecture PART 1.zip", "10.0.1", BaseTime.AddDays(2));
            
            // Updates to Part 2 (should not be matched)
            page.AddFile("Skyrim 202X 10.0 - Landscape PART 2", "10.0", BaseTime.AddDays(1));
        });

        var oldArchitecturePart = modPage.Files[0];
        var result = RunUpdateCheck.GetNewerFilesForExistingFile(oldArchitecturePart).ToArray();

        result.Should().HaveCount(2);
        result.Select(f => f.Version).Should().BeEquivalentTo("10.0", "10.0.1");
    }
}
