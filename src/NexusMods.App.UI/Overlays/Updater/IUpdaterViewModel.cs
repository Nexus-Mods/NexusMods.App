using NexusMods.App.BuildInfo;
using R3;

namespace NexusMods.App.UI.Overlays.Updater;

public interface IUpdaterViewModel : IOverlayViewModel
{
    ReactiveCommand CommandClose { get; }
    ReactiveCommand CommandOpenReleaseInBrowser { get; }
    ReactiveCommand CommandDownloadReleaseAssetInBrowser { get; }
    bool HasAsset { get; }

    Version CurrentVersion { get; }
    Version LatestVersion { get; }
    InstallationMethod InstallationMethod { get; }
}
