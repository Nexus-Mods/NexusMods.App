using NexusMods.Abstractions.GameLocators;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string to a locatable game.
/// </summary>
public class LocatableGameParser(IEnumerable<ILocatableGame> locatableGames) : IOptionParser<ILocatableGame>
{
    /// <inheritdoc />
    public bool TryParse(string toParse, out ILocatableGame value, out string error)
    {
        foreach (var locatableGame in locatableGames)
        {
            // Try to match the game name with the input string, either directly or via removing the spaces. 
            // This allows us to use "stardewvalley" instead of "Stardew Valley".
            if (!locatableGame.Name.Equals(toParse, StringComparison.OrdinalIgnoreCase)
                && !locatableGame.Name.Replace(" ", "").Equals(toParse, StringComparison.OrdinalIgnoreCase)) 
                continue;
            value = locatableGame;
            error = string.Empty;
            return true;
        }
        
        value = default!;
        error = $"The game '{toParse}' is not supported.";
        return false;
    }
}
