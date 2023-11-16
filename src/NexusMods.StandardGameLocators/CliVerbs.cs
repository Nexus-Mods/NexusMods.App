using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Games;
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
            .AddVerb(() => ListGames);

    [Verb("add-game", "Manually register a game in the database")]
    private static async Task<int> AddGame([Injected] IEnumerable<IGameLocator> locators,
        [Injected] IRenderer renderer,
        [Option("g", "game", "The game type to register")] IGame game,
        [Option("v", "version", "The game version to register")] Version version,
        [Option("p", "path", "The path where the game can be found")] AbsolutePath path)
    {
        var locator = locators.OfType<ManuallyAddedLocator>().First();
        locator.Add(game, version, path);
        await renderer.Text("Game added successfully");
        return 0;
    }

    [Verb("list-games", "List all games that are currently supported and installed.")]
    private static async Task<int> ListGames([Injected] IRenderer renderer, [Injected] IEnumerable<IGame> games)
    {
        var rows = from game in games
            from install in game.Installations
            orderby game.Name, install.Version
            select new object[]
            {
                game.Name, install.Version, install.Store, install.LocationsRegister.GetResolvedPath(LocationId.Game)
            };

        await renderer.Table(new[] {"Game", "Version", "Store", "Location"}, rows);
        return 0;
    }
}
