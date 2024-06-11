using Bannerlord.ModuleManager;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class ModExtensions
{
    public static MnemonicDB.SubModuleFileMetadata.ReadOnly? GetSubModuleFileMetadata(this Mod.ReadOnly mod)
    {
        // TODO: Implement this
        throw new NotImplementedException();
        //return MnemonicDB.SubModuleFileMetadata.OfSubModuleFileMetadata(mod.Files).FirstOrDefault();
    }

    public static MnemonicDB.ModuleInfoExtended.ReadOnly? GetModuleInfo(this Mod.ReadOnly mod) => 
        GetSubModuleFileMetadata(mod)?.ModuleInfo;

    public static IEnumerable<ModuleFileMetadata> GetModuleFileMetadatas(this Mod.ReadOnly mod)
    {
        throw new NotImplementedException();
        //return mod.Files.Values.Select(GetModuleFileMetadata).OfType<ModuleFileMetadata>();
    }

    public static ModuleFileMetadata? GetModuleFileMetadata(this File.ReadOnly modFile)
    {
        throw new NotImplementedException();
        //return modFile.Metadata.OfType<ModuleFileMetadata>().FirstOrDefault();
    }

    public static string? GetOriginalRelativePath(this File.ReadOnly mod) => GetModuleFileMetadata(mod)?.OriginalRelativePath;
}
