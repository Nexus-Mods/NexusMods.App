using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.OptionParsers;

public class ModListMarkerParser : IOptionParser<LoadoutMarker>
{
    private readonly LoadoutManager _manager;

    public ModListMarkerParser(LoadoutManager manager)
    {
        _manager = manager;
    }

    public LoadoutMarker Parse(string input, OptionDefinition<LoadoutMarker> definition)
    {
        return _manager.AllModLists.First(m =>
            m.Value.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase));
    }
}