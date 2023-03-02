using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.CLI.OptionParsers;

public class LoadoutParser : IOptionParser<Loadout>
{
    private readonly IDataStore _store;

    public LoadoutParser(IDataStore store)
    {
        _store = store;
    }

    public Loadout Parse(string input, OptionDefinition<Loadout> definition)
    {
        var bytes = Convert.FromHexString(input);
        var found = _store.GetByPrefix<Loadout>(new IdVariableLength(EntityCategory.Loadouts, bytes)).ToArray();
        if (found.Length > 1)
            throw new Exception("More than one Loadout with that id prefix found");

        return found.First();
    }

    public IEnumerable<string> GetOptions(string input)
    {
        return Array.Empty<string>();
    }
}
