using Bannerlord.ModuleManager;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class ModExtensions
{
    public static ModuleInfoExtended? GetModuleInfo(this Mod mod) => mod.Files.SelectMany(y => y.Value.Metadata).OfType<ModuleInfoMetadata>().FirstOrDefault() is { } metadata
        ? metadata.ModuleInfo
        : null;
    
    public static string? GetOriginalRelativePath(this Mod mod) => mod.Files.SelectMany(y => y.Value.Metadata).OfType<OriginalPathMetadata>().FirstOrDefault() is { } metadata
        ? metadata.OriginalRelativePath
        : null;
}