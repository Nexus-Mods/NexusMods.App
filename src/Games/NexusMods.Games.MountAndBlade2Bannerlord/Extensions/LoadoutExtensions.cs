using Bannerlord.LauncherManager;
using Bannerlord.LauncherManager.Models;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

internal delegate LoadoutModuleViewModel ViewModelCreator(Mod.ReadOnly mod, ModuleInfoExtendedWithPath moduleInfo, int index);

internal static class LoadoutExtensions
{
    private static LoadoutModuleViewModel Default(Mod.ReadOnly mod, ModuleInfoExtendedWithPath moduleInfo, int index) => new()
    {
        Mod = mod,
        ModuleInfoExtended = moduleInfo,
        // TODO: Actually implement this
        IsValid = true,
        IsSelected = mod.Enabled,
        IsDisabled = mod.Status == ModStatus.Failed,
        Index = index,
    };

    private static async Task<IEnumerable<Mod.ReadOnly>> SortMods(Loadout.ReadOnly loadout)
    {
        var loadoutSynchronizer = (((IGame)loadout.InstallationInstance.Game).Synchronizer as MountAndBlade2BannerlordLoadoutSynchronizer)!;

        var sorted = await loadoutSynchronizer.SortMods(loadout);
        return sorted;
    }

    public static IEnumerable<LoadoutModuleViewModel> GetViewModels(this Loadout.ReadOnly loadout, IEnumerable<Mod.ReadOnly> mods, ViewModelCreator? viewModelCreator = null)
    {
        viewModelCreator ??= Default;
        var i = 0;
        return mods.Select(x =>
        {
            var moduleInfo = x.GetModuleInfo();
            if (moduleInfo is null) return null;

            var subModule = x.Files.First(y => y.To.FileName.Path.Equals(Constants.SubModuleName, StringComparison.OrdinalIgnoreCase));
            var subModulePath = loadout.InstallationInstance.LocationsRegister.GetResolvedPath(subModule.To).GetFullPath();

            return viewModelCreator(x, moduleInfo.Value.FromEntity(), i++);
        }).OfType<LoadoutModuleViewModel>();
    }

    public static async Task<IEnumerable<LoadoutModuleViewModel>> GetSortedViewModelsAsync(this Loadout.ReadOnly loadout, ViewModelCreator? viewModelCreator = null)
    {
        var sortedMods = await SortMods(loadout);
        return GetViewModels(loadout, sortedMods, viewModelCreator);
    }

    public static IEnumerable<LoadoutModuleViewModel> GetViewModels(this Loadout.ReadOnly loadout, ViewModelCreator? viewModelCreator = null)
    {
        return GetViewModels(loadout, loadout.Mods, viewModelCreator);
    }

    public static bool HasModuleInstalled(this Loadout.ReadOnly loadout, string moduleId) => loadout.Mods.Any(x =>
        x.GetModuleInfo() is { } moduleInfo && moduleInfo.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

    public static bool HasInstalledFile(this Loadout.ReadOnly loadout, string filename) => loadout.Mods.Any(x =>
        x.GetModuleFileMetadatas().Any(y => y.OriginalRelativePath.EndsWith(filename, StringComparison.OrdinalIgnoreCase)));
}
