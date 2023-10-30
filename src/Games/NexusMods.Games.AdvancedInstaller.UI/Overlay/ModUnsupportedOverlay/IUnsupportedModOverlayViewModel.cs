using System.Windows.Input;
using NexusMods.App.UI.Overlays;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IUnsupportedModOverlayViewModel : IOverlayViewModel
{
    public string ModName { get; }

    /// <summary>
    ///     Declares whether the mod should be 'advanced installed'.
    ///     If this is false, the advanced installer should not be executed, else this should be set to true.
    /// </summary>
    public bool ShouldAdvancedInstall { get; set; }

    /// <summary>
    ///     Command to accept the offer to install the mod.
    /// </summary>
    ICommand AcceptCommand => ReactiveCommand.Create(Accept);

    /// <summary>
    ///     Command to decline the offer to install the mod.
    /// </summary>
    ICommand DeclineCommand => ReactiveCommand.Create(Decline);

    /// <summary>
    ///     Accepts the offer to install the mod, meaning the 'advanced installer' should be shown.
    /// </summary>
    public void Accept()
    {
        ShouldAdvancedInstall = true;
        IsActive = false;
    }

    /// <summary>
    ///     Declines the offer to install the mod, meaning the 'advanced installer' should be skipped.
    /// </summary>
    public void Decline()
    {
        ShouldAdvancedInstall = false;
        IsActive = false;
    }
}
