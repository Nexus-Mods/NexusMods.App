using Bannerlord.ModuleManager;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class ModExtensions
{
    public static SubModuleFileMetadata? GetSubModuleFileMetadata(this Mod mod) => mod.Files.SelectMany(y => y.Value.Metadata).OfType<SubModuleFileMetadata>().FirstOrDefault();
    public static ModuleInfoExtended? GetModuleInfo(this Mod mod) => GetSubModuleFileMetadata(mod)?.ModuleInfo;

    public static IEnumerable<ModuleFileMetadata> GetModuleFileMetadatas(this Mod mod) => mod.Files.Values.Select(GetModuleFileMetadata).OfType<ModuleFileMetadata>();
    public static ModuleFileMetadata? GetModuleFileMetadata(this AModFile modFile) => modFile.Metadata.OfType<ModuleFileMetadata>().FirstOrDefault();
    public static string? GetOriginalRelativePath(this AModFile mod) => GetModuleFileMetadata(mod)?.OriginalRelativePath;
}
