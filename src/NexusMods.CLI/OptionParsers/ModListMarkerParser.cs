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
        if (LoadoutId.TryParseFromHex(input, out var parsedId) && _registry.Contains(parsedId))
        {
            return _registry.GetMarker(parsedId);
        }

        var found = _registry.GetByName(input).Select(l => l.LoadoutId)
            .ToArray();
        if (found.Length == 0)
        {
            throw new Exception($"No loadout found with name or id {input}");
        }
        else if (found.Length > 1)
        {
            throw new Exception($"Multiple loadouts found with name {input}");
        }
        else if (found.Length == 1)
        {
            return _registry.GetMarker(found[0]);
        }
        else
        {
            throw new Exception($"No loadout found with name {input}");
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetOptions(string input)
    {
        var byName = _registry.AllLoadouts()
            .Where(l => l.Name.Contains(input, StringComparison.InvariantCultureIgnoreCase));
        return byName.Select(t => t.Name);
    }
}
