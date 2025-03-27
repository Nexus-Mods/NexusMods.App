using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.Networking.GitHub;
using NexusMods.Networking.GitHub.DTOs;
using NexusMods.Paths;

namespace NexusMods.App.UI.Overlays.Updater;

public class UpdateChecker
{
    private const string GitHubOrganization = "Nexus-Mods";
    private const string GitHubRepository = "NexusMods.App";

    private readonly ILogger _logger;
    private readonly IGitHubApi _gitHubApi;

    public UpdateChecker(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<UpdateChecker>>();
        _gitHubApi = serviceProvider.GetRequiredService<IGitHubApi>();
    }

    public static bool ShouldCheckForUpdate() => ShouldCheckForUpdate(ApplicationConstants.Version, ApplicationConstants.InstallationMethod);

    public static bool ShouldCheckForUpdate(Version currentVersion, InstallationMethod installationMethod)
    {
        // NOTE(erri120): don't check for updates if build from source
        return installationMethod is not InstallationMethod.Manually;
    }

    public async ValueTask<Release?> FetchUpdateRelease(Version currentVersion, CancellationToken cancellationToken = default)
    {
        var latestRelease = await _gitHubApi.FetchLatestRelease(GitHubOrganization, GitHubRepository, comparer: ReleaseTagComparer.Instance, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (latestRelease is null) return null;

        if (!latestRelease.TryGetVersion(out var latestVersion))
        {
            _logger.LogWarning("Failed to parse release tag `{TagName}` as a version for release `{ReleaseName}`", latestRelease.TagName, latestRelease.Name);
            return null;
        }

        if (latestVersion.Equals(currentVersion))
        {
            _logger.LogInformation("Latest version on GitHub `{LatestVersion}` matches current version `{CurrentVersion}`", latestVersion, currentVersion);
            return null;
        }

        _logger.LogInformation("Latest version on GitHub `{LatestVersion}` is newer than current version `{CurrentVersion}`", latestVersion, currentVersion);
        return latestRelease;
    }

    public bool TryGetMatchingReleaseAsset(Release release, IOSInformation os, InstallationMethod installationMethod, [NotNullWhen(true)] out Asset? releaseAsset)
    {
        if (release.Assets.Count == 0)
        {
            releaseAsset = null;
            return false;
        }

        foreach (var asset in release.Assets)
        {
            if (!Matches(asset.Name, os, installationMethod)) continue;

            releaseAsset = asset;
            return true;
        }

        releaseAsset = null;
        return false;
    }

    private static readonly Extension ArchiveExtension = new(".zip");
    private static readonly Extension InnoSetupExtension = new(".exe");
    private static readonly Extension AppImageExtension = new(".AppImage");

    internal static bool Matches(RelativePath fileName, IOSInformation os, InstallationMethod installationMethod)
    {
        if (installationMethod is InstallationMethod.InnoSetup && os.IsWindows) return fileName.Extension.Equals(InnoSetupExtension);
        if (installationMethod is InstallationMethod.AppImage && os.IsLinux) return fileName.Extension.Equals(AppImageExtension);

        if (installationMethod is InstallationMethod.Archive)
        {
            if (!fileName.Extension.Equals(ArchiveExtension)) return false;
            if (os.IsWindows) return fileName.Path.Contains(".win-", StringComparison.OrdinalIgnoreCase);
            if (os.IsLinux) return fileName.Path.Contains(".linux-", StringComparison.OrdinalIgnoreCase);

            // not supported
            return false;
        }

        // not supported
        return false;
    }
}
