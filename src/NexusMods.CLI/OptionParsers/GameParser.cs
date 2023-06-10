using NexusMods.DataModel.Games;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into an <see cref="IGame"/>
/// </summary>
public class GameParser : IOptionParser<IGame>
{
    private readonly IGame[] _games;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="games"></param>
    public GameParser(IEnumerable<IGame> games) => _games = games.ToArray();

    /// <inheritdoc />
    public IGame Parse(string input, OptionDefinition<IGame> definition)
    {
        return _games.FirstOrDefault(g => g.Domain == input) ??
               _games.FirstOrDefault(g => g.Name.Equals(input, StringComparison.CurrentCultureIgnoreCase))!;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _games.Where(g => g.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        var bySlug = _games.Where(g =>
            g.Domain.Value.Replace("-", "").Contains(input, StringComparison.CurrentCultureIgnoreCase));

        var found = byName.Concat(bySlug).Select(s => s.Domain.Value).Distinct();
        // ReSharper disable PossibleMultipleEnumeration
        return !found.Any() ? _games.Select(g => g.Domain.Value) : found;
        // ReSharper restore PossibleMultipleEnumeration
    }
}
