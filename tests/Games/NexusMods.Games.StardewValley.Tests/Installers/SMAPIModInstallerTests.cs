using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.Loadouts;
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

    [Fact]
    public async Task Test_Priority_WithoutManifest()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        await using var path = await CreateTestArchive(testFiles);

        var priority = await GetPriorityFromInstaller(path);
        priority.Should().Be(Priority.None);
    }

    [Fact]
    public async Task Test_Priority_WithManifest()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { "manifest.json", TestHelper.CreateManifest(out _) }
        };

        await using var path = await CreateTestArchive(testFiles);

        var priority = await GetPriorityFromInstaller(path);
        priority.Should().Be(Priority.Highest);
    }

    [Fact]
    public async Task Test_GetFilesToExtract()
    {
        var manifestFile = TestHelper.CreateManifest(out var modName);
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { $"{modName}/manifest.json", manifestFile },
            { $"{modName}/foo", Array.Empty<byte>() },
        };

        await using var path = await CreateTestArchive(testFiles);

        var (_, modFiles) = await GetModWithFilesFromInstaller(path);
        modFiles.Should().HaveCount(2);
        modFiles.Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/manifest.json"));
        modFiles.Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/foo"));
    }

    [Fact]
    public async Task Test_GetFilesToExtract_NestedArchive()
    {
        var manifestFile = TestHelper.CreateManifest(out var modName);
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { $"foo/bar/{modName}/manifest.json", manifestFile },
            { $"foo/bar/{modName}/baz", Array.Empty<byte>() }
        };

        await using var path = await CreateTestArchive(testFiles);

        var (_, modFiles) = await GetModWithFilesFromInstaller(path);
        modFiles.Should().HaveCount(2);
        modFiles.Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/manifest.json"));
        modFiles.Should().Contain(x => x.To.Path.Equals($"Mods/{modName}/baz"));
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_InstallMod()
    {
        var loadout = await CreateLoadout();

        // NPC Map Locations 2.11.3 (https://www.nexusmods.com/stardewvalley/mods/239)
        var (path, hash) = await DownloadMod(GameInstallation.Game.Domain, ModId.From(239), FileId.From(68865));
        await using (path)
        {
            hash.Should().Be(Hash.From(0x59112FD2E58BD042));

            var mod = await InstallModFromArchiveIntoLoadout(loadout, path);
            mod.Files.Should().NotBeEmpty();
            mod.Files.Should().AllSatisfy(kv => kv.Value.To.Path.StartsWith("Mods/NPCMapLocations"));
        }
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_MultipleModsOneArchive()
    {
        var loadout = await CreateLoadout();

        // Raised Garden Beds 1.0.5 (https://www.nexusmods.com/stardewvalley/mods/5305)
        var (path, hash) = await DownloadMod(GameInstallation.Game.Domain, ModId.From(5305), FileId.From(68056));
        await using (path)
        {
            hash.Should().Be(Hash.From(0xCBE810C4B0C82C7A));

            // var mods = await GetModsFromInstaller(path);
            var mods = await InstallModsFromArchiveIntoLoadout(loadout, path);
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
}
