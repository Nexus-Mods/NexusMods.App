using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.OptionParsers;

public class LoadoutMarkerParser : IOptionParser<LoadoutMarker>
{
    private readonly LoadoutManager _manager;

    public LoadoutMarkerParser(LoadoutManager manager) => _manager = manager;

    public LoadoutMarker Parse(string input, OptionDefinition<LoadoutMarker> definition)
    {
        var loadout = _manager.Registry.GetByName(input);
        return new LoadoutMarker(_manager, loadout!.LoadoutId);
    }

    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _manager.Registry.AllLoadouts()
            .Where(l => l.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        return byName.Select(t => t.Name);
    }
}
