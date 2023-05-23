using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.CLI.Tests.VerbTests;

[Trait("RequiresNetworking", "True")]
public class DownloadAndInstallMod : AGameTest<StubbedGame>
{
    public AVerbTest Test { get; }
    
    // Note: These tests use game testing framework to ensure code reuse.
    // This is needed because some APIs, e.g. loadouts require an actual game instance.
    public DownloadAndInstallMod(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Test = new AVerbTest(serviceProvider.GetRequiredService<TemporaryFileManager>(), serviceProvider);
    }

    // Not sure what to use for test data, we don't have a designated location,
    // and Nexus doesn't have raw download links.
    
    // For now I settled on stubbed mod from commit they were added in to the repo.
    // This should be valid as long as the repo is not renamed or commits deleted.
    // I think it's okay.
    [Theory]
    [InlineData("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/819887b5c2b575f395d50db60fe576f024c56092/tests/Games/NexusMods.Games.BethesdaGameStudios.Tests/Assets/DownloadableMods/RootedAtGameFolder/-Skyrim%20202X%209.0%20-%20Architecture-2347-9-0-1664994366.zip")]
    [InlineData("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/819887b5c2b575f395d50db60fe576f024c56092/tests/Games/NexusMods.Games.BethesdaGameStudios.Tests/Assets/DownloadableMods/RootedAtDataFolder/-Skyrim%20202X%209.0%20to%209.4%20-%20Update%20Ravenrock.zip")]
    [InlineData("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/819887b5c2b575f395d50db60fe576f024c56092/tests/Games/NexusMods.Games.BethesdaGameStudios.Tests/Assets/DownloadableMods/HasEsp_InSubfolder/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/819887b5c2b575f395d50db60fe576f024c56092/tests/Games/NexusMods.Games.BethesdaGameStudios.Tests/Assets/DownloadableMods/HasEsp/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("https://raw.githubusercontent.com/Nexus-Mods/NexusMods.App/819887b5c2b575f395d50db60fe576f024c56092/tests/Games/NexusMods.Games.BethesdaGameStudios.Tests/Assets/DownloadableMods/DataFolderWithDifferentName/-Skyrim%20202X%209.0%20to%209.4%20-%20Update%20Ravenrock.zip")]
    public async Task DownloadModFromUrl(string url)
    {
        var loadout = await CreateLoadout();
        var loadoutName = loadout.Value.Name;
        var origNumMods = loadout.Value.Mods.Count;
        origNumMods.Should().Be(1); // game files
        await Test.RunNoBanner("download-and-install-mod", "-u", url, "-l", loadoutName, "-n", "TestMod");
        loadout.Value.Mods.Count.Should().BeGreaterThan(origNumMods);
    }
}
