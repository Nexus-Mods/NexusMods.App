using FluentAssertions;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.LibraryArchiveInstallerTests;

public class RedModInstallerTests : ALibraryArchiveInstallerTests<Cyberpunk2077Game>
{
    public RedModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }


    [Theory]
    [InlineData("several_red_mods.7z")]
    [InlineData("one_mod.7z")]
    public async Task CanInstallRedMod(string filename)
    {
        var fullPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("LibraryArchiveInstallerTests/Resources/" + filename);

        var loadout = await CreateLoadoutOld();
        var libraryArchive = await RegisterLocalArchive(fullPath);
        var installResult = await Install(typeof(RedModInstaller), loadout, libraryArchive);
        installResult.Length.Should().Be(1, "The installer should have installed one group of files.");
        

        installResult.First().TryGetAsLoadoutItemGroup(out var group).Should().BeTrue("The installed result should be a loadout item group.");

        foreach (var child in group.Children)
        {
            child.TryGetAsLoadoutItemGroup(out var childGroup).Should().BeTrue("The child should be a loadout item group.");
            childGroup.TryGetAsRedModLoadoutGroup(out var redModGroup).Should().BeTrue("The child should be a red mod loadout group.");
        }
        
        await VerifyTx(installResult[0].MostRecentTxId()).UseParameters(filename);
    }
}
