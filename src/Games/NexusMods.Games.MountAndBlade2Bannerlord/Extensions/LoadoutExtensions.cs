using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Models;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal static class LoadoutExtensions
{
    private static async Task<IEnumerable<Mod>> SortMods(Loadout loadout)
    {
        var loadoutSynchronizer = (loadout.Installation.Game.Synchronizer as MountAndBlade2BannerlordLoadoutSynchronizer)!;

        var sorted = await loadoutSynchronizer.SortMods(loadout);
        return sorted;
    }

    public static IEnumerable<LoadoutModuleViewModel> GetViewModels(this Loadout loadout, IEnumerable<Mod> mods)
    {
        var i = 0;
        return mods.Select(x =>
        {
            var moduleInfo = x.GetModuleInfo();
            if (moduleInfo is null) return null;

            var subModule = x.Files.Values.OfType<StoredFile>().First(y => y.To.FileName.Path.Equals(Constants.SubModuleName, StringComparison.OrdinalIgnoreCase));

            return new LoadoutModuleViewModel
            {
                ModId = x.Id,
                Mod = x,
                ModuleInfoExtended = new ModuleInfoExtendedWithPath(moduleInfo, loadout.Installation.LocationsRegister.GetResolvedPath(subModule.To).GetFullPath()),
                IsSelected = true,
                IsDisabled = false,
                Index = i++
            };
        }).OfType<LoadoutModuleViewModel>();
    }

    public static async Task<IEnumerable<LoadoutModuleViewModel>> GetSortedViewModelsAsync(this Loadout loadout)
    {
        var sortedMods = await SortMods(loadout);
        return GetViewModels(loadout, sortedMods);
    }

    public static IEnumerable<LoadoutModuleViewModel> GetViewModels(this Loadout loadout)
    {
        return GetViewModels(loadout, loadout.Mods.Values);
    }

    public static bool HasModuleInstalled(this Loadout loadout, string moduleId) => loadout.Mods.Any(x =>
        x.Value.GetModuleInfo() is { } moduleInfo && moduleInfo.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

    public static bool HasInstalledFile(this Loadout loadout, string filename) => loadout.Mods.Any(x =>
        x.Value.GetOriginalRelativePath() is { } originalRelativePath && originalRelativePath.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
}
