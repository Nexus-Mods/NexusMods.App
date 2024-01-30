using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Models;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal delegate LoadoutModuleViewModel ViewModelCreator(Mod mod, ModuleInfoExtendedWithPath moduleInfo, int index);

internal static class LoadoutExtensions
{
    private static LoadoutModuleViewModel Default(Mod mod, ModuleInfoExtendedWithPath moduleInfo, int index) => new()
    {
        Mod = mod,
        ModuleInfoExtended = moduleInfo,
        IsValid = mod.GetSubModuleFileMetadata()?.IsValid == true,
        IsSelected = mod.Enabled,
        IsDisabled = mod.Status == ModStatus.Failed,
        Index = index,
    };

    private static async Task<IEnumerable<Mod>> SortMods(Loadout loadout)
    {
        var loadoutSynchronizer = (((IGame)loadout.Installation.Game).Synchronizer as MountAndBlade2BannerlordLoadoutSynchronizer)!;

        var sorted = await loadoutSynchronizer.SortMods(loadout);
        return sorted;
    }

    public static IEnumerable<LoadoutModuleViewModel> GetViewModels(this Loadout loadout, IEnumerable<Mod> mods, ViewModelCreator? viewModelCreator = null)
    {
        viewModelCreator ??= Default;
        var i = 0;
        return mods.Select(x =>
        {
            var moduleInfo = x.GetModuleInfo();
            if (moduleInfo is null) return null;

            var subModule = x.Files.Values.OfType<StoredFile>().First(y => y.To.FileName.Path.Equals(Constants.SubModuleName, StringComparison.OrdinalIgnoreCase));
            var subModulePath = loadout.Installation.LocationsRegister.GetResolvedPath(subModule.To).GetFullPath();

            return viewModelCreator(x, new ModuleInfoExtendedWithPath(moduleInfo, subModulePath), i++);
        }).OfType<LoadoutModuleViewModel>();
    }

    public static async Task<IEnumerable<LoadoutModuleViewModel>> GetSortedViewModelsAsync(this Loadout loadout, ViewModelCreator? viewModelCreator = null)
    {
        var sortedMods = await SortMods(loadout);
        return GetViewModels(loadout, sortedMods, viewModelCreator);
    }

    public static IEnumerable<LoadoutModuleViewModel> GetViewModels(this Loadout loadout, ViewModelCreator? viewModelCreator = null)
    {
        return GetViewModels(loadout, loadout.Mods.Values, viewModelCreator);
    }

    public static bool HasModuleInstalled(this Loadout loadout, string moduleId) => loadout.Mods.Values.Any(x =>
        x.GetModuleInfo() is { } moduleInfo && moduleInfo.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

    public static bool HasInstalledFile(this Loadout loadout, string filename) => loadout.Mods.Values.Any(x =>
        x.GetModuleFileMetadatas().Any(y => y.OriginalRelativePath.EndsWith(filename, StringComparison.OrdinalIgnoreCase)));
}
