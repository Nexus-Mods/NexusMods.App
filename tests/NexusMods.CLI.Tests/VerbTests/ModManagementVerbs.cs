using FluentAssertions;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.CLI.Tests.VerbTests;

public class ModManagementVerbs(StubbedGame stubbedGame, IServiceProvider provider) : AVerbTest(provider)
{
    
    [Fact]
    public async Task CanCreateAndManageLists()
    {
        var listName = Guid.NewGuid().ToString();
        
        var install = await CreateInstall();

        var log = await Run("loadout create", "-g", $"{uint.MaxValue}", "-v",
            install.Version.ToString(), "-n", listName);

        log = await Run("loadouts list");

        log.LastTableColumns.Should().BeEquivalentTo("Name", "Game", "Id", "Items");
        log.TableCellsWith(0, listName).Should().NotBeEmpty();

        log = await Run("loadout groups list", "-l", listName);
        log.LastTable.Rows.Length.Should().Be(2);

        log = await Run("loadout install", "-l", listName, "-f", Data7ZipLZMA2.ToString(), "-n", Data7ZipLZMA2.GetFileNameWithoutExtension());

        log = await Run("loadout groups list", "-l", listName);
        log.LastTable.Rows.Length.Should().Be(3);

        log = await Run("loadout group list", "-l", listName, "-g", Data7ZipLZMA2.FileName);
        log.LastTable.Rows.Length.Should().Be(3);
        
        log = await Run("loadout synchronize", "-l", listName);
    }
}
