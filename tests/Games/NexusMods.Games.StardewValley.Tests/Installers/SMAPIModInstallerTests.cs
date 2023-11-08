using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NexusMods.CLI.Verbs;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

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
        modFiles.Cast<IToFile>().Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/manifest.json"));
        modFiles.Cast<IToFile>().Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/foo"));
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_InstallMod()
    {
        var loadout = await CreateLoadout();

        // NPC Map Locations 2.11.3 (https://www.nexusmods.com/stardewvalley/mods/239)
        var downloadId = await DownloadMod(GameInstallation.Game.Domain, ModId.From(239), FileId.From(68865));

        var mod = await InstallModStoredFileIntoLoadout(loadout, downloadId);
        mod.Files.Should().NotBeEmpty();
        mod.Files.Values.Cast<IToFile>().Should().AllSatisfy(kv => kv.To.Path.StartsWith("Mods/NPCMapLocations"));
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
        mods
            .Should().HaveCount(3)
            .And.AllSatisfy(x =>
            {
                x.Metadata.Should().BeOfType<GroupMetadata>();
                x.Version.Should().Be("1.0.5");
            })
            .And.Satisfy(
                x => x.Name == "Raised Garden Beds",
                x => x.Name == "[CP] Raised Garden Beds Translation: English",
                x => x.Name == "[RGB] Raised Garden Beds"
            );

    }
}
