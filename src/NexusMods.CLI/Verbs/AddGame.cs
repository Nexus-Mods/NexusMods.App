using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;

namespace NexusMods.CLI.Verbs;

/// <summary>
/// Manually register a game in the database
/// </summary>
public class AddGame : AVerb<IGame, Version, AbsolutePath>
{
    private readonly ManuallyAddedLocator _manualLocator;
    private readonly ILogger<AddGame> _addGame;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="addGame"></param>
    /// <param name="games"></param>
    public AddGame(ILogger<AddGame> addGame, IEnumerable<IGameLocator> games)
    {
        _addGame = addGame;
        _manualLocator = games.OfType<ManuallyAddedLocator>().First();
    }

    /// <inheritdoc />
    public static VerbDefinition Definition => new VerbDefinition("add-game",
        "Manually register a game in the database", new OptionDefinition[]
        {
            new OptionDefinition<IGame>("g", "game", "The game to add"),
            new OptionDefinition<Version>("v", "version", "The game version"),
            new OptionDefinition<AbsolutePath>("p", "path", "The game installation path")
        });

    /// <inheritdoc />
    public Task<int> Run(IGame game, Version version, AbsolutePath path, CancellationToken token)
    {
        _manualLocator.Add(game, version, path);
        _addGame.LogInformation("Game added successfully");
        return Task.FromResult(0);
    }
}
