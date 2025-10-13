using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Games.AdvancedInstaller.UI.Preview;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;
using NexusMods.UI.Sdk;

namespace NexusMods.Games.AdvancedInstaller.UI;

/// <summary>
/// Main body of the Advanced Manual Installer UI. Drives most of the UI logic.
/// </summary>
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

    /// <summary>
    /// ViewModel for the left content area, showing the contents of the mod archive.
    /// </summary>
    public IModContentViewModel ModContentViewModel { get; }

    /// <summary>
    /// Empty preview view model, shown in the right area when there are no files to install.
    /// </summary>
    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; }

    /// <summary>
    /// Select location view model, shown in the right area when the user needs to select a location to install files to.
    /// </summary>
    public ISelectLocationViewModel SelectLocationViewModel { get; }

    /// <summary>
    /// Mod preview view model, shown in the right area if there are files ready for install.
    /// </summary>
    public IPreviewViewModel PreviewViewModel { get; }

    /// <summary>
    /// Contains the relevant information for installation of the files.
    /// Updated when user changes the mappings in the UI.
    /// </summary>
    public DeploymentData DeploymentData { get; }
}
