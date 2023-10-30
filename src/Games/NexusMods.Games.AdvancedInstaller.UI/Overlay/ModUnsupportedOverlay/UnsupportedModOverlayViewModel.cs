using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class UnsupportedModOverlayViewModel : AViewModel<IUnsupportedModOverlayViewModel>,
    IUnsupportedModOverlayViewModel
{
    public UnsupportedModOverlayViewModel(string modName)
    {
        ModName = modName;
    }

    public string ModName { get; }
    [Reactive] public bool IsActive { get; set; }
    [Reactive] public bool ShouldAdvancedInstall { get; set; } = false;
}
