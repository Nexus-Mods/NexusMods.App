using FluentAssertions;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.LibraryArchiveInstallerTests;

public class RedModInstallerTests : ALibraryArchiveInstallerTests
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

        var loadout = await CreateLoadout();
        var libraryArchive = await RegisterLocalArchive(fullPath);
        var installer = await Install(typeof(RedModInstaller), loadout, libraryArchive);
        installer.Length.Should().Be(1, "The installer should have installed one group of files.");

        await VerifyTx(installer[0].MostRecentTxId()).UseParameters(filename);
    }
}
