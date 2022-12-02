using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.Markers;

namespace NexusMods.CLI.OptionParsers;

public class ModListMarkerParser : IOptionParser<ModListMarker>
{
    private readonly ModListManager _manager;

    public ModListMarkerParser(ModListManager manager)
    {
        _manager = manager;
    }

    public ModListMarker Parse(string input, OptionDefinition<ModListMarker> definition)
    {
        return _manager.AllModLists.First(m =>
            m.Value.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase));
    }
}