using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI.Settings;
using NexusMods.CrossPlatform.Process;
using NexusMods.Networking.GitHub;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Overlays.Updater;

[UsedImplicitly]
public class UpdaterViewModel : AOverlayViewModel<IUpdaterViewModel>, IUpdaterViewModel
{
    public ReactiveCommand CommandClose { get; }
    public ReactiveCommand CommandOpenReleaseInBrowser { get; }
    public ReactiveCommand CommandDownloadReleaseAssetInBrowser { get; }
    public bool HasAsset { get; }
    public Version CurrentVersion { get; }
    public Version LatestVersion { get; }
    public InstallationMethod InstallationMethod { get; }

    internal UpdaterViewModel(
        IOSInterop osInterop,
        ISettingsManager settingsManager,
        InstallationMethod installationMethod,
        Version currentVersion,
        Version latestVersion,
        Uri releaseWebUri,
        Uri? assetDownloadUri)
    {
        CommandClose = new ReactiveCommand(_ =>
        {
            // NOTE(erri120): something for later if we want to skip
            // settingsManager.Set(new UpdaterSettings
            // {
            //     VersionToSkip = latestVersion,
            // });

            base.Close();
        });

        CommandOpenReleaseInBrowser = new ReactiveCommand(_ => osInterop.OpenUrl(releaseWebUri));
        CommandDownloadReleaseAssetInBrowser = Observable.Return(assetDownloadUri is not null).ToReactiveCommand(_ => osInterop.OpenUrl(assetDownloadUri!));

        HasAsset = assetDownloadUri is not null;
        CurrentVersion = currentVersion;
        LatestVersion = latestVersion;
        InstallationMethod = installationMethod;
    }

    public static async ValueTask<IUpdaterViewModel?> CreateIfNeeded(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var currentVersion = ApplicationConstants.Version;
        var installationMethod = ApplicationConstants.InstallationMethod;
        var os = OSInformation.Shared;

        if (!UpdateChecker.ShouldCheckForUpdate(currentVersion, installationMethod)) return null;
        var updateChecker = serviceProvider.GetRequiredService<UpdateChecker>();
        var settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();

        var release = await updateChecker.FetchUpdateRelease(currentVersion, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (release is null) return null;

        if (!release.TryGetVersion(out var latestVersion)) throw new UnreachableException();
        if (settingsManager.Get<UpdaterSettings>().VersionToSkip.Equals(latestVersion)) return null;

        updateChecker.TryGetMatchingReleaseAsset(release, os, installationMethod, out var releaseAsset);

        return new UpdaterViewModel(
            osInterop: serviceProvider.GetRequiredService<IOSInterop>(),
            settingsManager: settingsManager,
            installationMethod: installationMethod,
            currentVersion: currentVersion,
            latestVersion: latestVersion,
            releaseWebUri: new Uri(release.HtmlUrl),
            assetDownloadUri: releaseAsset is null ? null : new Uri(releaseAsset.BrowserDownloadUrl)
        );
    }
}
