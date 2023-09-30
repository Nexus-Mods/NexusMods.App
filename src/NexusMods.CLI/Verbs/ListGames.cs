using NexusMods.Abstractions.CLI;
using NexusMods.Abstractions.CLI.DataOutputs;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Lists all the installed games
/// </summary>
public class ListGames : AVerb, IRenderingVerb
{
    private readonly IEnumerable<IGame> _games;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <inheritdoc />
    public static VerbDefinition Definition => new("list-games",
        "Lists all the installed games",
        Array.Empty<OptionDefinition>());

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="games"></param>
    public ListGames(IEnumerable<IGame> games)
    {
        _games = games;
    }

    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<int> Run(CancellationToken token)
    {
        var installs = from game in _games.OrderBy(g => g.Name)
                       from install in game.Installations.OrderBy(g => g.Version)
                       select install;

        await Renderer.Render(
            new Table(new[] { "Game", "Version", "Path", "Store" },
                installs.Select(i => new object[] { i.Game, i.Version, i.LocationsRegister[LocationId.Game], i.Store })));

        return 0;
    }
}
