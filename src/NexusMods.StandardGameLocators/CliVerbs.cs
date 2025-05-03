using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Verbs for the game locators
/// </summary>
internal static class CliVerbs
{
    internal static IServiceCollection AddGameLocatorCliVerbs(this IServiceCollection services) =>
        services.AddVerb(() => AddGame)
            .AddVerb(() => RemoveGame)
            .AddVerb(() => ListGames);

    [Verb("add-game", "Manually register a game in the database")]
    private static async Task<int> AddGame([Injected] IEnumerable<IGameLocator> locators,
        [Injected] IRenderer renderer,
        [Option("g", "game", "The game type to register")] IGame game,
        [Option("v", "version", "The game version to register")] Version version,
        [Option("p", "path", "The path where the game can be found")] AbsolutePath path)
    {
        var locator = locators.OfType<ManuallyAddedLocator>().First();
        var manualInfo = await locator.Add(game, version, path);
        await renderer.Text("Game added successfully. Save this ID somewhere for future reference: {0}", manualInfo.Item1);
        return 0;
    }

    [Verb("remove-game", "Manually unregister a manually-added game from the database")]
    private static async Task<int> RemoveGame(
        [Injected] IEnumerable<IGameLocator> locators,
        [Injected] IRenderer renderer,
        [Injected] IConnection conn,
        [Option("g", "game", "The game to unregister")] IGame game,
        [Option("id", "entityID", "The EntityId of the game to remove")]
        string id)
    {
        var locator = locators.OfType<ManuallyAddedLocator>().First();
        var entId = EntityId.From(Convert.ToUInt64(id, 16));
        var db = conn.Db;
        // Removes loadouts associated with this game first
        var managedInstallations = Loadout.All(db)
            .Select(loadout => loadout.InstallationInstance)
            .Distinct();
        foreach (var installation in managedInstallations)
        {
            if (installation.Locator != locator || installation.Game != game) continue;
            var synchronizer = installation.GetGame().Synchronizer;
            await synchronizer.UnManage(installation, false);
        }
        // Remove the game from the database
        await locator.Remove(entId);
        await renderer.Text("Game removed successfully.");
        return 0;
    }

    [Verb("list-games", "List all games that are currently supported and installed.")]
    private static async Task<int> ListGames(
        [Injected] IRenderer renderer, 
        [Injected] IEnumerable<IGame> games,
        [Injected] IGameRegistry registry)
    {
        var rows = from game in games
            from install in registry.Installations.Values
            where game.GameId == install.Game.GameId
            orderby game.Name
            select new object[]
            {
                game.Name, install.Game.GameId, install.Store, install.LocationsRegister.GetResolvedPath(LocationId.Game)
            };

        await renderer.Table(new[] {"Game", "GameId", "Store", "Location"}, rows);
        return 0;
    }
}
