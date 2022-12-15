using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ModLists;

namespace NexusMods.CLI.OptionParsers;

public class ModListParser : IOptionParser<ModList>
{
    private readonly IDataStore _store;

    public ModListParser(IDataStore store)
    {
        _store = store;
    }
    
    public ModList Parse(string input, OptionDefinition<ModList> definition)
    {
        var bytes = Convert.FromHexString(input);
        var found = _store.GetByPrefix<ModList>(new IdVariableLength(EntityCategory.ModLists, bytes)).ToArray();
        if (found.Length > 1)
            throw new Exception("More than one modlist with that id prefix found");
        return found.First();
    }
}