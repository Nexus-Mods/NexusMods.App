using Bannerlord.LauncherManager.Models;
using NexusMods.DataModel.Loadouts;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;
using NexusMods.Paths;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class LoadoutExtensions
{
    public static IEnumerable<LoadoutModuleViewModel> GetViewModels(this Loadout loadout)
    {
        var gamePath = loadout.Installation.LocationsRegister[LocationId.Game];
        var i = 0;
        return loadout.Mods.Select(x =>
        {
            var moduleInfo = x.Value.GetModuleInfo();
            if (moduleInfo is null) return null;
            return new LoadoutModuleViewModel
            {
                ModId = x.Key,
                Mod = x.Value,
                // TODO: Installation Path
                ModuleInfoExtended = new ModuleInfoExtendedWithPath(moduleInfo, ""),
                IsSelected = true,
                IsDisabled = false,
                Index = i++
            };
        }).OfType<LoadoutModuleViewModel>();
    }

    public static bool HasModuleInstalled(this Loadout loadout, string moduleId) => loadout.Mods.Any(x =>
        x.Value.GetModuleInfo() is { } moduleInfo && moduleInfo.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

    public static bool HasInstalledFile(this Loadout loadout, string filename) => loadout.Mods.Any(x =>
        x.Value.GetOriginalRelativePath() is { } originalRelativePath && originalRelativePath.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
}
