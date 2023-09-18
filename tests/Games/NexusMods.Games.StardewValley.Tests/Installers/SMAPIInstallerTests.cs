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


    /* DISABLED until we fix the SMAPI installer
    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_GetFilesToExtract()
    {
        var loadout = await CreateLoadout();

        // SMAPI 3.18.2 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        var downloadId = await DownloadMod(StardewValley.GameDomain, ModId.From(2400), FileId.From(64874));
        var mod = await InstallModFromArchiveIntoLoadout(loadout, downloadId);

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
    */
}
