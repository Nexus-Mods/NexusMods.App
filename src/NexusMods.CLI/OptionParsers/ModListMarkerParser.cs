using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.OptionParsers;

public class LoadoutMarkerParser : IOptionParser<LoadoutMarker>
{
    private readonly LoadoutRegistry _registry;

    public LoadoutMarkerParser(LoadoutRegistry manager) => _registry = manager;

    public LoadoutMarker Parse(string input, OptionDefinition<LoadoutMarker> definition)
    {
        var loadout = _registry.GetByName(input);
        return new LoadoutMarker(_registry, loadout!.LoadoutId);
    }

    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _registry.AllLoadouts()
            .Where(l => l.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        return byName.Select(t => t.Name);
    }
}
