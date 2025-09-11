using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Games.CreationEngine.SkyrimSE;

public static class SkyrimSEKnownPaths
{
    public static readonly GamePath AppDataPath = new(LocationId.AppData, "Skyrim Special Edition");
    public static readonly GamePath PluginsFile = AppDataPath / "plugins.txt";
}
