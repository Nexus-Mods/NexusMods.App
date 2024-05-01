using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into a loadout
/// </summary>
internal class LoadoutParser : IOptionParser<Loadout.Model>
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

    public bool TryParse(string toParse, out Loadout value, out string error)
    {
        var bytes = Convert.FromHexString(toParse);
        var found = _store.GetByPrefix<Loadout>(new IdVariableLength(EntityCategory.Loadouts, bytes)).ToArray();
        if (found.Length > 1)
            throw new Exception("More than one Loadout with that id prefix found");

        value = found.First();
        error = string.Empty;
        return true;
    }
}
