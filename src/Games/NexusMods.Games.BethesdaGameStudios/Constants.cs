using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Constants
{
    public static readonly GamePath DataFolder = new(LocationId.Game, "Data".ToRelativePath());
}
