using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an <see cref="IGame"/>
/// </summary>
internal class GameParser(IServiceProvider serviceProvider) : IOptionParser<IGame>
{
    public bool TryParse(string toParse, out IGame value, out string error)
    {
        var games = serviceProvider.GetServices<IGameData>().ToArray();
        var game = games.FirstOrDefault(x => x.DisplayName.Equals(toParse, StringComparison.OrdinalIgnoreCase));
        if (game is null && ulong.TryParse(toParse, out var parsedGameId))
        {
            game = games.FirstOrDefault(x => x.GameId == parsedGameId);
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

