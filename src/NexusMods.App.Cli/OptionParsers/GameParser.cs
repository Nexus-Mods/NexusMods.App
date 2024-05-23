using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an <see cref="IGame"/>
/// </summary>
internal class GameParser(IGameRegistry gameRegistry) : IOptionParser<IGame>
{
    public bool TryParse(string toParse, out IGame value, out string error)
    {
        var install = gameRegistry.Installations.Values.FirstOrDefault(g => g.Game.Domain == toParse) ??
                    gameRegistry.Installations.Values.FirstOrDefault(g => g.Game.Name.Equals(toParse, StringComparison.CurrentCultureIgnoreCase))!;

        value = (IGame)install.Game;
        error = string.Empty;
        return true;
    }
}
