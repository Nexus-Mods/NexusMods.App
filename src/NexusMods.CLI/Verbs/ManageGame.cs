using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
/// <summary>
/// Manage a game
/// </summary>
public class ManageGame : AVerb<IGame, Version, string>, IRenderingVerb
{
    private readonly LoadoutManager _manager;

    /// <inheritdoc />
    public IRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="configurator"></param>
    public ManageGame(LoadoutManager manager)
    {
        _manager = manager;
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new("manage-game", "Manage a game",
        new OptionDefinition[]
        {
            new OptionDefinition<IGame>("g", "game", "Game to manage"),
            new OptionDefinition<Version>("v", "version", "Version of the game to manage"),
            new OptionDefinition<string>("n", "name", "Name of the new Loadout")
        });

    /// <inheritdoc />
    public async Task<int> Run(IGame game, Version version, string name, CancellationToken token)
    {
        var installation = game.Installations.FirstOrDefault(i => i.Version == version);
        if (installation == null)
            throw new Exception("Game not found");

        await Renderer.WithProgress(token, async () =>
        {
            await _manager.ManageGameAsync(installation, name, token);
            return 0;
        });
        return 0;
    }
}
