using Bannerlord.LauncherManager.Models;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

internal class LoadoutModuleViewModel : IModuleViewModel
{
    public required LoadoutItem.ReadOnly Mod { get; init; }

    public required ModuleInfoExtendedWithMetadata ModuleInfoExtended { get; init; }

    public required bool IsValid { get; init; }

    public required bool IsSelected { get; set; }

    public required bool IsDisabled { get; set; }

    public required int Index { get; set; }
}
