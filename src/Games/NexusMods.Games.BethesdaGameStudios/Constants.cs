using NexusMods.DataModel.Abstractions.Games;
using NexusMods.DataModel.Games;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Constants
{
    public static GamePath DataFolder = new(LocationId.Game, "Data".ToRelativePath());
}
