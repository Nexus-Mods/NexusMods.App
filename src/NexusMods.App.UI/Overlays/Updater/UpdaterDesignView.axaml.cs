using Avalonia.Controls;
using JetBrains.Annotations;
using NexusMods.App.BuildInfo;

namespace NexusMods.App.UI.Overlays.Updater;

[UsedImplicitly]
public partial class UpdaterDesignView : UserControl
{
    public UpdaterDesignView()
    {
        InitializeComponent();

        ItemsControl.ItemsSource = new[]
        {
            Create(InstallationMethod.Archive, hasAsset: true),
            Create(InstallationMethod.InnoSetup, hasAsset: true),
            Create(InstallationMethod.AppImage, hasAsset: true),
            Create(InstallationMethod.Flatpak, hasAsset: false),
            Create(InstallationMethod.PackageManager, hasAsset: false),
        };
    }

    private static IUpdaterViewModel Create(InstallationMethod installationMethod, bool hasAsset)
    {
        return new UpdaterViewModel(
            osInterop: null!,
            settingsManager: null!,
            installationMethod: installationMethod,
            currentVersion: new Version("1.0.0"),
            latestVersion: new Version("2.0.0"),
            releaseWebUri: new Uri("https://example.com"),
            assetDownloadUri: hasAsset ? new Uri("https://example.com") : null
        );
    }
}

