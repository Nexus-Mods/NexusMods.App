using Mutagen.Bethesda.Plugins;

namespace NexusMods.Games.CreationEngine.Abstractions;

/// <summary>
/// A generic plugin header, so that the same code can be used to all Bethesda games.
/// </summary>
public struct PluginHeader
{
    public ModKey Key { get; init; }
    public ModKey[] Masters { get; init; }
}
