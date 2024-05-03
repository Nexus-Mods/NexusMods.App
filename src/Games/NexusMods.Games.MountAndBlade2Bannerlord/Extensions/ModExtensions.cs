﻿using Bannerlord.ModuleManager;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using File = NexusMods.Abstractions.Loadouts.Files.File;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class ModExtensions
{
    public static SubModuleFileMetadata? GetSubModuleFileMetadata(this Mod.Model mod)
    {
        return mod.Files.SelectMany(y => y.Metadata).OfType<SubModuleFileMetadata>().FirstOrDefault();
    }

    public static ModuleInfoExtended? GetModuleInfo(this Mod.Model mod) => GetSubModuleFileMetadata(mod)?.ModuleInfo;

    public static IEnumerable<ModuleFileMetadata> GetModuleFileMetadatas(this Mod.Model mod)
    {
        throw new NotImplementedException();
        //return mod.Files.Values.Select(GetModuleFileMetadata).OfType<ModuleFileMetadata>();
    }

    public static ModuleFileMetadata? GetModuleFileMetadata(this File.Model modFile)
    {
        throw new NotImplementedException();
        //return modFile.Metadata.OfType<ModuleFileMetadata>().FirstOrDefault();
    }

    public static string? GetOriginalRelativePath(this File.Model mod) => GetModuleFileMetadata(mod)?.OriginalRelativePath;
}
