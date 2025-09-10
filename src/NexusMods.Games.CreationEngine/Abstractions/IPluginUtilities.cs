using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine.Abstractions;

public interface IPluginUtilities
{
    /// <summary>
    /// Parses a plugin header from a hash. The hash should exist in the system's file store. The name
    /// is optional but provides a more useful header result. 
    /// </summary>
    public ValueTask<PluginHeader?> ParsePluginHeader(Hash hash, RelativePath? name = null);
}
