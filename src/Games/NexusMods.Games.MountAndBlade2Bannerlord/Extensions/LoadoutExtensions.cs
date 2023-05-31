using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class LoadoutExtensions
{
    public static bool HasModuleInstalled(this Loadout loadout, string moduleId) => loadout.Mods.Any(x =>
        x.Value.GetModuleInfo() is { } moduleInfo && moduleInfo.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

    public static bool HasInstalledFile(this Loadout loadout, string filename) => loadout.Mods.Any(x =>
        x.Value.GetOriginalRelativePath() is { } originalRelativePath && originalRelativePath.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
}
