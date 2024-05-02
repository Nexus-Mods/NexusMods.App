using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.ModId;

namespace NexusMods.Games.StardewValley.Tests.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SMAPIModInstallerTests : AModInstallerTest<StardewValley, SMAPIModInstaller>
{
    public SMAPIModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Theory]
    [InlineData("")]
    [InlineData("foo/bar/baz/")]
    [Trait("FlakeyTest", "True")]
    public async Task Test_GetMods(string basePath)
    {
        var manifestFile = TestHelper.CreateManifest(out var modName);
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { $"{basePath}{modName}/manifest.json", manifestFile },
            { $"{basePath}{modName}/foo", Array.Empty<byte>() },
        };

        await using var path = await CreateTestArchive(testFiles);

        var (_, modFiles) = await GetModWithFilesFromInstaller(path);
        modFiles.Should().HaveCount(2);
        modFiles.Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/manifest.json"));
        modFiles.Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/foo"));
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_InstallMod()
    {
        var loadout = await CreateLoadout();

        // NPC Map Locations 2.11.3 (https://www.nexusmods.com/stardewvalley/mods/239)
        var downloadId = await DownloadMod(GameInstallation.Game.Domain, ModId.From(239), FileId.From(68865));

        var mod = await InstallModStoredFileIntoLoadout(loadout, downloadId);
        await VerifyMod(mod);
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_MultipleModsOneArchive()
    {
        var loadout = await CreateLoadout();

        // Raised Garden Beds 1.0.5 (https://www.nexusmods.com/stardewvalley/mods/5305)
        var downloadId = await DownloadMod(GameInstallation.Game.Domain, ModId.From(5305), FileId.From(68056));

        // var mods = await GetModsFromInstaller(path);
        var mods = await InstallModsStoredFileIntoLoadout(loadout, downloadId);
        await VerifyMods(mods);
    }
}
