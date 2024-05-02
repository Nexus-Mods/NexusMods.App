using FluentAssertions;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.CLI.Tests.VerbTests;

[Trait("RequiresNetworking", "True")]
public class DownloadAndInstallMod(IServiceProvider serviceProvider, LocalHttpServer server) : AGameTest<StubbedGame>(serviceProvider)
{
    public AVerbTest Test { get; } = new(serviceProvider);

    // Note: These tests use game testing framework to ensure code reuse.
    // This is needed because some APIs, e.g. loadouts require an actual game instance.

    // Not sure what to use for test data, we don't have a designated location,
    // and Nexus doesn't have raw download links.

    // For now I settled on stubbed mod from commit they were added in to the repo.
    // This should be valid as long as the repo is not renamed or commits deleted.
    // I think it's okay.
    [Theory]
    [InlineData("Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip")]
    [InlineData("Resources/RootedAtDataFolder/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    [InlineData("Resources/HasEsp_InSubfolder/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/HasEsp/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/DataFolderWithDifferentName/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    public async Task DownloadModFromUrl(string url)
    {
        var loadout = await CreateLoadout();
        var origNumMods = loadout.Mods.Count;
        origNumMods.Should().Be(1); // game files

        var oldRevision = loadout.Revision;
        var oldLoadoutId = loadout.LoadoutId;
        var makeUrl = $"{server.Uri}{url}";
        
        await Test.Run("download-and-install-mod", "-u", makeUrl, "-l", oldLoadoutId.ToString(), "-n", "TestMod");
        loadout = Refresh(loadout);
        loadout.Revision.Should().NotBe(oldRevision, "the loadout has been updated");

        loadout.Mods.Count.Should().BeGreaterThan(origNumMods);
    }

    [Theory]
    [InlineData("cyberpunk2077", 107, 33156)]
    public async Task DownloadModFromNxm(string gameDomain, ulong modId, ulong fileId)
    {
        // This test requires Premium. If it fails w/o Premium, ignore that.
        var loadout = await CreateLoadout();
        var origNumMods = loadout.Mods.Count;
        origNumMods.Should().Be(1); // game files

        var uri = $"nxm://{gameDomain}/mods/{modId}/files/{fileId}";
        await Test.Run("download-and-install-mod", "-u", uri, "-l", loadout.Id.ToString(), "-n", "TestMod");
        loadout.Mods.Count.Should().BeGreaterThan(origNumMods);
    }
}
