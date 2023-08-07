using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into a loadout
/// </summary>
public class LoadoutParser : IOptionParser<Loadout>
{
    private readonly IDataStore _store;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="store"></param>
    public LoadoutParser(IDataStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public Loadout Parse(string input, OptionDefinition<Loadout> definition)
    {
        var bytes = Convert.FromHexString(input);
        var found = _store.GetByPrefix<Loadout>(new IdVariableLength(EntityCategory.Loadouts, bytes)).ToArray();
        if (found.Length > 1)
            throw new Exception("More than one Loadout with that id prefix found");

        return found.First();
    }

    /// <inheritdoc />
    public IEnumerable<string> GetOptions(string input)
    {
        return Array.Empty<string>();
    }
}
