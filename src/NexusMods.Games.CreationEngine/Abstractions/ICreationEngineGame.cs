using Mutagen.Bethesda.Plugins.Records;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine.Abstractions;


/// <summary>
/// A common interface for all games that use the creation engine.
/// </summary>
public interface ICreationEngineGame
{
    /// <summary>
    /// Parse a plugin file. Currently, this loads only the header of the file, in the future
    /// we can add flags to load specific groups
    /// </summary>
    public ValueTask<IMod?> ParsePlugin(Hash hash, RelativePath? name = null);
}
