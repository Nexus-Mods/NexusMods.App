using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

public static class LoadoutExtensions
{
    public static bool HasModuleInstalled(this Loadout loadout, string moduleId)
    {
        return loadout.Mods.Any(x => x.Value.Files.Any(y => y.Value.Metadata.OfType<ModuleIdMetadata>().FirstOrDefault() is { } metadata && metadata.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase)));
    }
    public static bool HasInstalledFile(this Loadout loadout, string filename)
    {
        return loadout.Mods.Any(x => x.Value.Files.Any(y => y.Value.Metadata.OfType<OriginalPathMetadata>().FirstOrDefault() is { } metadata && metadata.OriginalRelativePath.EndsWith(filename, StringComparison.OrdinalIgnoreCase)));
    }
}
