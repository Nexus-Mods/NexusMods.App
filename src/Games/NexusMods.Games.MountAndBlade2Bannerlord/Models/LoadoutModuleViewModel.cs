using Bannerlord.LauncherManager.Models;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.MnemonicDB;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

internal class LoadoutModuleViewModel : IModuleViewModel
{
    public required Mod.Model Mod { get; init; }

    public required ModuleInfoExtendedWithPath ModuleInfoExtended { get; init; }

    public required bool IsValid { get; init; }

    public required bool IsSelected { get; set; }

    public required bool IsDisabled { get; set; }

    public required int Index { get; set; }
}
