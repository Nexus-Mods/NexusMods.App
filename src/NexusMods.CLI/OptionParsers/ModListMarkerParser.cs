using NexusMods.Abstractions.CLI;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into a loadout marker
/// </summary>
public class LoadoutMarkerParser : IOptionParser<LoadoutMarker>
{
    private readonly LoadoutRegistry _registry;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="manager"></param>
    public LoadoutMarkerParser(LoadoutRegistry manager) => _registry = manager;

    /// <inheritdoc />
    public LoadoutMarker Parse(string input, OptionDefinition<LoadoutMarker> definition)
    {
        var loadout = _registry.GetByName(input);
        return new LoadoutMarker(_registry, loadout!.LoadoutId);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _registry.AllLoadouts()
            .Where(l => l.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        return byName.Select(t => t.Name);
    }
}
