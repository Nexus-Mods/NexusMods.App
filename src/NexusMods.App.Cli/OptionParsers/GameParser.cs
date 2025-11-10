using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an <see cref="IGame"/>
/// </summary>
internal class GameParser(IGameRegistry gameRegistry) : IOptionParser<IGame>
{
    public bool TryParse(string toParse, out IGame value, out string error)
    {
        var game = gameRegistry.SupportedGames.FirstOrDefault(g => g.DisplayName.Equals(toParse, StringComparison.OrdinalIgnoreCase));
        if (game is null && uint.TryParse(toParse, out var parsedGameId))
        {
            game = gameRegistry.SupportedGames.FirstOrDefault(g => g.GameId.Equals(parsedGameId));
        }
        
        if (game is null)
        {
            value = null!;
            error = $"Unknown game: `{toParse}`";
            return false;
        }
        
        value = (IGame)game;
        error = string.Empty;
        return true;
    }
}

