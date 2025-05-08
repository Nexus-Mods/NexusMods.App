using NexusMods.Abstractions.GameLocators;
namespace NexusMods.Games.RedEngine;

internal class Constants
{
    internal static readonly GamePath RedModPath = new(LocationId.Game, "tools/redmod/bin/redMod.exe");
    internal static readonly GamePath RedModInstallFolder = new(LocationId.Game, "mods");
    internal static readonly GamePath RedModDeployFolder = new(LocationId.Game, "r6/cache/modded");
}
