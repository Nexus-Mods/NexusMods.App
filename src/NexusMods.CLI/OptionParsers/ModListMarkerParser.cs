using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.CLI.OptionParsers;

/// <summary>
/// Parses a string into a loadout marker
/// </summary>
internal class LoadoutMarkerParser : IOptionParser<LoadoutMarker>
{
    private readonly LoadoutRegistry _registry;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="manager"></param>
    public LoadoutMarkerParser(LoadoutRegistry manager) => _registry = manager;

    public bool TryParse(string input, out LoadoutMarker value, out string error)
    {
        error = string.Empty;
        if (LoadoutId.TryParseFromHex(input, out var parsedId) && _registry.Contains(parsedId))
        {
            value = _registry.GetMarker(parsedId);
            return true;
        }

        var found = _registry.GetByName(input).Select(l => l.LoadoutId)
            .ToArray();

        switch (found.Length)
        {
            case 0:
                throw new Exception($"No loadout found with name or id {input}");
            case > 1:
                throw new Exception($"Multiple loadouts found with name {input}");
            case 1:
                value = _registry.GetMarker(found[0]);
                return true;
            default:
                throw new Exception($"No loadout found with name {input}");
        }
    }
}
