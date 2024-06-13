﻿using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;
using File = NexusMods.Abstractions.Loadouts.Files.File;
using ModuleFileMetadata = NexusMods.Games.MountAndBlade2Bannerlord.Models.ModuleFileMetadata;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class ModExtensions
{
    public static bool TryGetModuleInfo(this Mod.ReadOnly mod, out ModuleInfoExtended.ReadOnly moduleInfo)
    {
        var subModuleMetadata = mod.Files.OfTypeModuleFileMetadata()
            .OfTypeSubModuleFileMetadata()
            .FirstOrDefault();
        
        if (subModuleMetadata.IsValid())
        {
            moduleInfo = subModuleMetadata.ModuleInfo;
            return true;
        }

        moduleInfo = default(ModuleInfoExtended.ReadOnly);
        return false;
    }

    public static IEnumerable<ModuleFileMetadata> GetModuleFileMetadatas(this Mod.ReadOnly mod)
    {
        foreach (var file in mod.Files)
        {
            if (file.TryGetModuleFileMetadata(out var meta))
            {
                yield return meta;
            }
        }
    }

    public static bool TryGetModuleFileMetadata(this File.ReadOnly modFile, out ModuleFileMetadata data)
    {
        throw new NotImplementedException();
    }

    public static string? GetOriginalRelativePath(this File.ReadOnly mod)
    {
        throw new NotImplementedException();
    }
}
