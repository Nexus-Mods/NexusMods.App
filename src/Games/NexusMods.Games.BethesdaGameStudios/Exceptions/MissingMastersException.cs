using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Exceptions;

public class MissingMastersException(IEnumerable<(RelativePath Plugin, RelativePath Master)> missingMasters) :
    Exception(missingMasters
        .Select(m => $"\tPlugin {m.Plugin} requires master {m.Master}, but it is not present in the load order.\n")
        .Aggregate("Cannot sort plugins because: \n", (a, b) => a + b));
