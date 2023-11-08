using FluentAssertions;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.CLI.Tests.VerbTests;

public class ModManagementVerbs : AVerbTest
{
    private readonly StubbedGame _stubbedGame;

    public ModManagementVerbs(TemporaryFileManager temporaryFileManager, StubbedGame stubbedGame,
        IServiceProvider provider) : base(temporaryFileManager, provider)
    {
        _stubbedGame = stubbedGame;
    }

    [Fact]
    public async Task CanCreateAndManageLists()
    {
        var listName = Guid.NewGuid().ToString();

        await RunNoBannerAsync("manage-game", "-g", "stubbed-game", "-v",
            _stubbedGame.Installations.First().Version.ToString(), "-n", listName);

        await RunNoBannerAsync("list-managed-games");

        LastTable.Columns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        LastTable.Rows.FirstOrDefault(r => r.First().Equals(listName)).Should().NotBeNull();

        await RunNoBannerAsync("list-mods", "-l", listName);
        LastTable.Rows.Count().Should().Be(1);

        await RunNoBannerAsync("install-mod", "-l", listName, "-f", Data7ZipLZMA2.ToString(), "-n", Data7ZipLZMA2.GetFileNameWithoutExtension());

        await RunNoBannerAsync("list-mods", "-l", listName);
        LastTable.Rows.Count().Should().Be(2);

        await RunNoBannerAsync("list-mod-contents", "-l", listName, "-n", Data7ZipLZMA2.GetFileNameWithoutExtension());
        LastTable.Rows.Count().Should().Be(3);

        await RunNoBannerAsync("flatten-list", "-l", listName);
        LastTable.Rows.Count().Should().Be(7);

        await RunNoBannerAsync("apply", "-l", listName);
        LastLog.OfType<string>().Last().Should().Contain($"Applied {listName}");
    }
}
