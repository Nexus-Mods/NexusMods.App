using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Games.CreationEngine;

public static class KnownPaths
{
    public static readonly GamePath Game = new GamePath(LocationId.Game, "");
    public static readonly GamePath Data = new GamePath(LocationId.Game, "Data");
}
