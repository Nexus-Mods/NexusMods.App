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
        try
        {
            var install = gameRegistry.Installations.Values.FirstOrDefault(g => g.Game.GameId.ToString() == toParse) ??
                          gameRegistry.Installations.Values.FirstOrDefault(g => g.Game.Name.Equals(toParse, StringComparison.CurrentCultureIgnoreCase)) ??
                          GameInstallation.Empty;
            if (install.Equals(GameInstallation.Empty))
            {
                throw new NullReferenceException();
            }
            value = (IGame)install.Game;
            error = string.Empty;
            return true;
        }
        catch (NullReferenceException)
        {
            // Recheck if current game parse is supported
            var install = gameRegistry.SupportedGames.FirstOrDefault(g => g.GameId.ToString() == toParse) ??
                       gameRegistry.SupportedGames.FirstOrDefault(g => g.Name.Equals(toParse, StringComparison.CurrentCultureIgnoreCase))!;
            if (install == null!)
            {
                value = null!;
                error = $"Game '{toParse}' is not found or supported.";
                return false;
            }
            value = (IGame)install;
            error = $"Warning: Game '{toParse}' is supported but not installed. Please install it properly for full functionality.";
            return true;
        }
    }
}
