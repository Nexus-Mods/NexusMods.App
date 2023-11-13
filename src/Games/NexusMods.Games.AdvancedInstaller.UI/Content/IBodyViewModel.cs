using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IBodyViewModel : IViewModelInterface
{
    /// <summary>
    /// Name of the mod to be installed.
    /// Used for the title of the window.
    /// </summary>
    public string ModName { get; set; }

    /// <summary>
    /// Whether there are any files ready to be installed or not.
    /// Determines whether the Install button is enabled or not.
    /// </summary>
    public bool CanInstall { get; }

    /// <summary>
    /// Which ViewModel is in use for the right content area.
    /// Can be either <see cref="PreviewViewModel"/>, <see cref="SelectLocationViewModel"/> or <see cref="EmptyPreviewViewModel"/>.
    /// </summary>
    public IViewModelInterface CurrentRightContentViewModel { get; }

    public IModContentViewModel ModContentViewModel { get; }

    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; }

    public ISelectLocationViewModel SelectLocationViewModel { get; }

    public IPreviewViewModel PreviewViewModel { get; }

    public DeploymentData DeploymentData { get; }
}
