using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Exceptions;

/// <summary>
/// Thrown when sorting plugins, and one of the plugins is missing a master
/// </summary>
/// <param name="missingMasters"></param>
public class MissingMastersException(IEnumerable<(RelativePath Master, RelativePath Plugin)> missingMasters) :
    Exception(missingMasters
        .Select(m => $"\tPlugin {m.Plugin} requires master {m.Master}, but it is not present in the load order.\n")
        .Aggregate("Cannot sort plugins because: \n", (a, b) => a + b));
