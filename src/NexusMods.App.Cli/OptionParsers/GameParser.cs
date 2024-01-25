using NexusMods.Abstractions.Games;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an <see cref="IGame"/>
/// </summary>
internal class GameParser : IOptionParser<IGame>
{
    private readonly IGame[] _games;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="games"></param>
    public GameParser(IEnumerable<IGame> games) => _games = games.ToArray();


    public bool TryParse(string toParse, out IGame value, out string error)
    {
        value = _games.FirstOrDefault(g => g.Domain == toParse) ??
                    _games.FirstOrDefault(g => g.Name.Equals(toParse, StringComparison.CurrentCultureIgnoreCase))!;
        error = string.Empty;
        return true;
    }
}
