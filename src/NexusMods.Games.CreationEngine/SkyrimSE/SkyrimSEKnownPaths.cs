using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public static class SkyrimSEKnownPaths
{
    public static readonly GamePath PluginsTxt = new(LocationId.AppData, "Skyrim Special Edition/plugins.txt");
    
}
