using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class UnsupportedModOverlayViewModel : AViewModel<IUnsupportedModOverlayViewModel>,
    IUnsupportedModOverlayViewModel
{
    [Reactive] public string ModName { get; set; } = string.Empty;
    [Reactive] public bool IsActive { get; set; }
    [Reactive] public bool ShouldAdvancedInstall { get; set; } = false;
}
