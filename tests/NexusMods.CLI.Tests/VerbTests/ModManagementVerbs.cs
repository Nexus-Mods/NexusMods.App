using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.CLI.Tests.VerbTests;

public class ModManagementVerbs : AVerbTest
{
    private readonly ILogger<ModManagementVerbs> _logger;
    
    public ModManagementVerbs(IServiceProvider provider) : base(provider)
    {
        _logger = provider.GetRequiredService<ILogger<ModManagementVerbs>>();
    }
    
    [Fact]
    public async Task CanCreateAndManageLists()
    {
        var listName = Guid.NewGuid().ToString();
        
        var install = await CreateInstall();

        var log = await Run("create-loadout", "-g", "stubbed-game", "-v",
            install.Version.ToString(), "-n", listName);

        log = await Run("list-loadouts");

        log.LastTableColumns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        log.TableCellsWith(0, listName).Should().NotBeEmpty();

        log = await Run("list-mods", "-l", listName);
        log.LastTable.Rows.Length.Should().Be(1);

        log = await Run("install-mod", "-l", listName, "-f", Data7ZipLZMA2.ToString(), "-n", Data7ZipLZMA2.GetFileNameWithoutExtension());

        log = await Run("list-mods", "-l", listName);
        log.LastTable.Rows.Length.Should().Be(2);
        var modNameString = log.LastTable.Rows[1].ToString() ?? "no cells";
        _logger.LogDebug("Second row: {ModNameString}",modNameString);
        

        log = await Run("list-mod-contents", "-l", listName, "-m", Data7ZipLZMA2.GetFileNameWithoutExtension());
        log.LastTable.Rows.Length.Should().Be(3);

        log = await Run("flatten-loadout", "-l", listName);
        await VerifyLog(log, "flatten-loadout");
        
        log = await Run("apply", "-l", listName);
        
        log.Last<Text>().Template.Should().Contain($"Applied {listName}");
    }
}
