using Mutagen.Bethesda.Plugins.Records;
using NexusMods.Abstractions.Games;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;


using NexusMods.Sdk.Games;

namespace NexusMods.Games.CreationEngine.Abstractions;

/// <summary>
/// A common interface for all games that use the creation engine.
/// </summary>
public interface ICreationEngineGame : IGame
{
    public GamePath PluginsFile { get; }
    /// <summary>
    /// Parse a plugin file. Currently, this loads only the header of the file, in the future
    /// we can add flags to load specific groups
    /// </summary>
    public ValueTask<IMod?> ParsePlugin(Hash hash, RelativePath? name = null);
}
