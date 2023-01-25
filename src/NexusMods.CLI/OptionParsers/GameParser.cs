using NexusMods.DataModel.Games;

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
        return _games.FirstOrDefault(g => g.Domain == input) ??
               _games.FirstOrDefault(g => g.Name.Equals(input, StringComparison.CurrentCultureIgnoreCase))!;
    }

    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _games.Where(g => g.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        var bySlug = _games.Where(g =>
            g.Domain.Value.Replace("-", "").Contains(input, StringComparison.CurrentCultureIgnoreCase));

        var found = byName.Concat(bySlug).Select(s => s.Domain.Value).Distinct();
        return !found.Any() ? _games.Select(g => g.Domain.Value) : found;
    }
}