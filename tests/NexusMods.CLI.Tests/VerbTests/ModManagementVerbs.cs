using FluentAssertions;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests.VerbTests;

public class ModManagementVerbs : AVerbTest
{
    public ModManagementVerbs(TemporaryFileManager temporaryFileManager, IServiceProvider provider) : base(temporaryFileManager, provider)
    {
    }

    [Fact]
    public async Task CanCreateAndManageLists()
    {
        var listName = Guid.NewGuid().ToString();

        await RunNoBanner("manage-game", "-g", "stubbed-game", "-v", "0.0.0.0", "-n", listName);

        await RunNoBanner("list-managed-games");

        LastTable.Columns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        LastTable.Rows.FirstOrDefault(r => r.First().Equals(listName)).Should().NotBeNull();

        await RunNoBanner("list-mods", "-m", listName);
        LastTable.Rows.Count().Should().Be(1);

        await RunNoBanner("install-mod", "-m", listName, "-f", Data7ZipLZMA2.ToString());
        
        await RunNoBanner("list-mods", "-m", listName);
        LastTable.Rows.Count().Should().Be(2);

        await RunNoBanner("list-mod-contents", "-m", listName, "-n", Data7ZipLZMA2.FileName.ToString());
        LastTable.Rows.Count().Should().Be(3);
        
        await RunNoBanner("flatten-list", "-m", listName);
        LastTable.Rows.Count().Should().Be(7);
    }
}