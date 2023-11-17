using Bannerlord.LauncherManager.Models;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

internal class LoadoutModuleViewModel : IModuleViewModel
{
    public ModId ModId { get; init; }
    public Mod Mod { get; init; } = default!;

    public ModuleInfoExtendedWithPath ModuleInfoExtended { get; init; } = default!;

    public bool IsValid { get; init; } = true; // TODO:

    public bool IsSelected { get; set; } = true; // TODO:

    public bool IsDisabled { get; set; } = false;

    public int Index { get; set; }
}
