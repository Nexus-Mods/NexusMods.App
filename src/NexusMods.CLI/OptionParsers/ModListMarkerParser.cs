using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.OptionParsers;

public class LoadoutMarkerParser : IOptionParser<LoadoutMarker>
{
    private readonly LoadoutManager _manager;

    public LoadoutMarkerParser(LoadoutManager manager) => _manager = manager;

    public LoadoutMarker Parse(string input, OptionDefinition<LoadoutMarker> definition)
    {
        return _manager.AllLoadouts.First(m =>
            m.Value.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase));
    }

    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _manager.AllLoadouts
            .Where(l => l.Value.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        return byName.Select(t => t.Value.Name);
    }
}
