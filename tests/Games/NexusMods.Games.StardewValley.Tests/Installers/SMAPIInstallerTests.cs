using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.StardewValley.Tests.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SMAPIInstallerTests : AModInstallerTest<StardewValley, SMAPIInstaller>
{
    public SMAPIInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }


    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_GetFilesToExtract()
    {
        var loadout = await CreateLoadout();

        // SMAPI 3.18.2 (https://www.nexusmods.com/stardewvalley/mods/2400?tab=files)
        var downloadId = await DownloadMod(StardewValley.GameDomain, ModId.From(2400), FileId.From(64874));
        var mod = await InstallModStoredFileIntoLoadout(loadout, downloadId);

        var files = mod.Files;
        files.Should().NotBeEmpty();

        mod.Version.Should().Be("3.18.2");
    }
}
