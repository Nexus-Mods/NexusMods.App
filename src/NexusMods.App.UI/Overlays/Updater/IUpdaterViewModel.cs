using System.Windows.Input;
using NexusMods.Common;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.Updater;

/// <summary>
/// View model for the Updater overlay.
/// </summary>
public interface IUpdaterViewModel : IOverlayViewModel
{
    /// <summary>
    /// The installation method of the running application
    /// </summary>
    public InstallationMethod Method { get; }

    /// <summary>
    /// The new version of the application
    /// </summary>
    public Version NewVersion { get; }

    /// <summary>
    /// The current version of the application
    /// </summary>
    public Version OldVersion { get; }

    /// <summary>
    /// The command to call to start downloading the update via a downloader
    /// </summary>
    public ICommand UpdateCommand { get; }

    /// <summary>
    /// Closes the overlay without updating
    /// </summary>
    public ICommand LaterCommand { get; }

    /// <summary>
    /// The command to show the changelog
    /// </summary>
    public ICommand ShowChangelog { get; }

    /// <summary>
    /// The UI should show a message about the system updater. For example AppImage users should be told to use the
    /// system updater and not hand download the update
    /// </summary>

    public bool ShowSystemUpdateMessage { get; }

    /// <summary>
    /// Show the overlay if if required, return true if it was shown.
    /// </summary>
    /// <returns></returns>
    public Task<bool> MaybeShow();
}
