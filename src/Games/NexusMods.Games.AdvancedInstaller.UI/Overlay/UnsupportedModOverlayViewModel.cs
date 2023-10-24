namespace NexusMods.Games.AdvancedInstaller.UI;

public class UnsupportedModOverlayViewModel : AViewModel<IUnsupportedModOverlayViewModel>, IUnsupportedModOverlayViewModel
{
    public bool IsActive { get; set; }
    public bool ShouldAdvancedInstall { get; set; } = false;
}
