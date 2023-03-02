using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.Verbs;

// ReSharper disable once ClassNeverInstantiated.Global
public class ManageGame : AVerb<IGame, Version, string>
{
    private readonly LoadoutManager _manager;
    private readonly IRenderer _renderer;

    public ManageGame(LoadoutManager manager, Configurator configurator)
    {
        _manager = manager;
        _renderer = configurator.Renderer;
    }

    public static VerbDefinition Definition => new("manage-game", "Manage a game",
        new OptionDefinition[]
        {
            new OptionDefinition<IGame>("g", "game", "Game to manage"),
            new OptionDefinition<Version>("v", "version", "Version of the game to manage"),
            new OptionDefinition<string>("n", "name", "Name of the new Loadout")
        });

    public async Task<int> Run(IGame game, Version version, string name, CancellationToken token)
    {
        var installation = game.Installations.FirstOrDefault(i => i.Version == version);
        if (installation == null)
            throw new Exception("Game not found");

        await _renderer.WithProgress(token, async () =>
        {
            await _manager.ManageGame(installation, name, token);
            return 0;
        });
        return 0;
    }
}
