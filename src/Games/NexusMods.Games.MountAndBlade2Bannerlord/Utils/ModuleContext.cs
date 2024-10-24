using Bannerlord.LauncherManager;
using Bannerlord.ModuleManager;
using NexusMods.Games.MountAndBlade2Bannerlord.Models;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Utils;

internal class ModuleContext
{
    private readonly IDictionary<string, LoadoutModuleViewModel> _lookup;
    public ModuleContext(IEnumerable<LoadoutModuleViewModel> moduleVMs)
    {
        _lookup = moduleVMs.ToDictionary(x => x.ModuleInfoExtended.Id, x => x);
    }
    public ModuleContext(IDictionary<string, LoadoutModuleViewModel> lookup)
    {
        _lookup = lookup;
    }

    public bool GetIsValid(ModuleInfoExtended module)
    {
        if (FeatureIds.LauncherFeatures.Contains(module.Id))
            return true;

        return _lookup[module.Id].IsValid;
    }
    public bool GetIsSelected(ModuleInfoExtended module)
    {
        if (FeatureIds.LauncherFeatures.Contains(module.Id))
            return false;

        return _lookup[module.Id].IsSelected;
    }
    public void SetIsSelected(ModuleInfoExtended module, bool value)
    {
        if (FeatureIds.LauncherFeatures.Contains(module.Id))
            return;
        _lookup[module.Id].IsSelected = value;
    }
    public bool GetIsDisabled(ModuleInfoExtended module)
    {
        if (FeatureIds.LauncherFeatures.Contains(module.Id))
            return false;

        return _lookup[module.Id].IsDisabled;
    }
    public void SetIsDisabled(ModuleInfoExtended module, bool value)
    {
        if (FeatureIds.LauncherFeatures.Contains(module.Id))
            return;
        _lookup[module.Id].IsDisabled = value;
    }
}
