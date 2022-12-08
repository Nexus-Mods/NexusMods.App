using NexusMods.Interfaces.Components;

namespace NexusMods.CLI.OptionParsers;

public class GameParser : IOptionParser<IGame>
{
    private readonly IGame[] _games;

    public GameParser(IEnumerable<IGame> games)
    {
        _games = games.ToArray();
    }
    
    public IGame Parse(string input, OptionDefinition<IGame> definition)
    {
        return _games.FirstOrDefault(g => g.Slug == input) ??
               _games.FirstOrDefault(g => g.Name.Equals(input, StringComparison.CurrentCultureIgnoreCase))!;
    }
}