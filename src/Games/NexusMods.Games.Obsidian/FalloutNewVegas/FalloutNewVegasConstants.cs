using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.Obsidian.FalloutNewVegas;

internal class FalloutNewVegasConstants
{
    internal static readonly GamePath NVSEPath = new(LocationId.Game, "nvse_loader.exe");
    public static readonly RelativePath DataFolder = "Data".ToRelativePath();
}
