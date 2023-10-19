using NexusMods.App.UI.Overlays;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IUnsupportedModOverlayViewModel : IOverlayViewModel
{
    /// <summary>
    ///     Declares whether the mod should be 'advanced installed'.
    ///     If this is false, the advanced installer should not be executed, else this should be set to true.
    /// </summary>
    public bool ShouldAdvancedInstall { get; set; }
}
