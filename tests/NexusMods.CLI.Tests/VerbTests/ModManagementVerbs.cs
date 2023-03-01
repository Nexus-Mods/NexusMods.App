using FluentAssertions;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.CLI.Tests.VerbTests;

public class ModManagementVerbs : AVerbTest
{
    private readonly StubbedGame _stubbedGame;

    public ModManagementVerbs(TemporaryFileManager temporaryFileManager, StubbedGame stubbedGame, IServiceProvider provider) : base(temporaryFileManager, provider)
    {
        _stubbedGame = stubbedGame;
    }

    [Fact]
    public async Task CanCreateAndManageLists()
    {
        var listName = Guid.NewGuid().ToString();

        await RunNoBanner("manage-game", "-g", "stubbed-game", "-v", _stubbedGame.Installations.First().Version.ToString(), "-n", listName);

        await RunNoBanner("list-managed-games");

        LastTable.Columns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        LastTable.Rows.FirstOrDefault(r => r.First().Equals(listName)).Should().NotBeNull();

        await RunNoBanner("list-mods", "-l", listName);
        LastTable.Rows.Count().Should().Be(1);

        await RunNoBanner("install-mod", "-l", listName, "-f", Data7ZipLZMA2.ToString());
        
        await RunNoBanner("list-mods", "-l", listName);
        LastTable.Rows.Count().Should().Be(2);

        await RunNoBanner("list-mod-contents", "-l", listName, "-n", Data7ZipLZMA2.FileName);
        LastTable.Rows.Count().Should().Be(3);
        
        await RunNoBanner("flatten-list", "-l", listName);
        LastTable.Rows.Count().Should().Be(7);
        
        await RunNoBanner("apply", "-l", listName, "-r", "false");
    }
}