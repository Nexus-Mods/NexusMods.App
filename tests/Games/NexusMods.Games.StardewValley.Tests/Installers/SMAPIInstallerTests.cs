using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NexusMods.Common;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley.Tests.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SMAPIInstallerTests : AModInstallerTest<StardewValley, SMAPIInstaller>
{
    public SMAPIInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_Priority_WithFiles()
    {
        var zipSignature = new byte[] {0x50, 0x4B, 0x03, 0x04};
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { "linux/install.dat", zipSignature },
            { "windows/install.dat", zipSignature },
            { "macOS/install.dat", zipSignature }
        };

        await using var path = await CreateTestArchive(testFiles);

        var priority = await GetPriorityFromInstaller(path.Path);
        priority.Should().Be(Priority.Highest);
    }

    [Fact]
    public async Task Test_Priority_WithoutFiles()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        await using var path = await CreateTestArchive(testFiles);

        var priority = await GetPriorityFromInstaller(path.Path);
        priority.Should().Be(Priority.None);
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_GetFilesToExtract()
    {
        var loadout = await CreateLoadout();

        // SMAPI 3.18.2 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        var (path, hash) = await DownloadMod(StardewValley.GameDomain, ModId.From(2400), FileId.From(64874));
        await using (path)
        {
            hash.Should().Be(Hash.From(0x8F3F6450139866F3));

            var mod = await InstallModFromArchiveIntoLoadout(loadout, hash);

            var files = mod.Files;
            files.Should().NotBeEmpty();
            files
                .Values
                .Cast<IToFile>()
                .Should().Contain(x => x.To.Path.Equals("StardewModdingAPI.deps.json"))
                .Which
                .Should().BeOfType<GameFile>();

            // TODO: update tests once the installer is working correctly
        }
    }
}
