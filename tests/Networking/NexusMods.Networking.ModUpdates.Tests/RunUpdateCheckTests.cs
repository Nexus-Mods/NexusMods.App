using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.NexusWebApi;
using Xunit.Abstractions;
using FileId = NexusMods.Abstractions.NexusWebApi.Types.V2.FileId;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.V2.ModId;

namespace NexusMods.Networking.ModUpdates.Tests;

public class RunUpdateCheckTests : ACyberpunkIsolatedGameTest<RunUpdateCheckTests>
{
    private readonly NexusModsLibrary _nexusModsLibrary;

    public RunUpdateCheckTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
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
        var modId = ModId.From(107u); // CET
        var fileId = FileId.From(18963u); // 1.18.1

        await using var tempFile = TemporaryFileManager.CreateFile();
        var downloadJob = await _nexusModsLibrary.CreateDownloadJob(
            destination: tempFile,
            gameDomain: Cyberpunk2077Game.StaticDomain,
            modId: modId,
            fileId: fileId
        );

        // install to loadout
        var libraryFile = await LibraryService.AddDownload(downloadJob);
        await LibraryService.InstallItem(libraryFile.AsLibraryItem(), loadout);
        
        // Ensure we're actually doing work
        var updates = await RunUpdateCheck.CheckForModPagesWhichNeedUpdating(Connection.Db, NexusNexusApiClient);
        
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
        newerMods.Should().HaveCountGreaterThan(42);
    }
}
