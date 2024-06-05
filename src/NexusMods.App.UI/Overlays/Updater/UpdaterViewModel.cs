using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform.Process;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Updater;

public class UpdaterViewModel : AOverlayViewModel<IUpdaterViewModel>, IUpdaterViewModel
{
    private Uri _githubRepo = new("https://api.github.com/repos/Nexus-Mods/NexusMods.App/releases");

    private readonly HttpClient _client;
    private readonly ILogger<UpdaterViewModel> _logger;
    private readonly IOverlayController _overlayController;

    [Reactive]
    public InstallationMethod Method { get; set; }

    [Reactive]
    public Version NewVersion { get; set; } = Version.Parse("0.0.0.0");

    [Reactive] public Version? OldVersion { get; set; }
    public ICommand UpdateCommand { get; }

    public ICommand LaterCommand { get; }

    public ICommand ShowChangelog { get; }

    [Reactive]
    public Uri UpdateUrl { get; set; } = new("https://github.com/Nexus-Mods/NexusMods.App/releases/latest");

    [Reactive]
    public Uri ChangelogUrl { get; set; } = new("https://github.com/Nexus-Mods/NexusMods.App/releases/latest");

    [Reactive] public bool ShowSystemUpdateMessage { get; set; } = false;


    public UpdaterViewModel(ILogger<UpdaterViewModel> logger, IOSInterop interop, HttpClient client, IOverlayController overlayController)
    {
        _client = client;
        _logger = logger;
        _overlayController = overlayController;
        OldVersion = ApplicationConstants.Version;
        Method = CompileConstants.InstallationMethod;

        LaterCommand = ReactiveCommand.Create(Close);

        UpdateCommand = ReactiveCommand.Create(() =>
        {
            interop.OpenUrl(UpdateUrl);
            Close();
        });

        ShowChangelog = ReactiveCommand.Create(() =>
        {
            interop.OpenUrl(ChangelogUrl);
        });
    }

    public async Task<bool> MaybeShow()
    {
        if (!await ShouldShow()) return false;

        _overlayController.Enqueue(this);
        return true;
    }

    public async Task<bool> ShouldShow()
    {
        _logger.LogInformation("Checking for updates from GitHub");
        try
        {
            if (Method == InstallationMethod.Manually) return false;

            var releases = await GetReleases();

            var latestRelease = releases.Where(r => r is { IsDraft: false, IsPrerelease: false })
                .MaxBy(r => r.Version);

            if (latestRelease is null)
            {
                _logger.LogInformation("No releases available");
                return false;
            }

            ChangelogUrl = latestRelease.HtmlUrl;

            if (latestRelease.Version < OldVersion)
            {
                _logger.LogInformation("No new release available");
                return false;
            }

            _logger.LogInformation("New version available: {Version}", latestRelease.Version);

            var asset = FindAsset(latestRelease);
            if (asset is null) return false;

            _logger.LogInformation("Asset found: {Asset}", asset.Name);
            UpdateUrl = asset.BrowserDownloadUrl;
            NewVersion = latestRelease.Version;

            if (Method is InstallationMethod.AppImage or InstallationMethod.PackageManager)
            {
                ShowSystemUpdateMessage = true;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return false;
        }

    }

    private Asset? FindAsset(Release latestRelease)
    {
        switch (Method)
        {
            case InstallationMethod.InnoSetup:
                return latestRelease.Assets.FirstOrDefault(r => r.Name.EndsWith(".exe"));
            case InstallationMethod.Archive when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                                                 RuntimeInformation.OSArchitecture == Architecture.X64:
                return latestRelease.Assets.FirstOrDefault(r => r.Name.EndsWith(".win-x64.zip"));
            case InstallationMethod.Archive when RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                                                 RuntimeInformation.OSArchitecture == Architecture.X64:
                return latestRelease.Assets.FirstOrDefault(r => r.Name.EndsWith(".linux-x64.zip"));
            case InstallationMethod.AppImage:
                return latestRelease.Assets.FirstOrDefault(r => r.Name.EndsWith(".AppImage"));
            default:
                return null;
        }
    }

    private async Task<Release[]> GetReleases()
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, _githubRepo);
        msg.Headers.Add("User-Agent", "NexusMods.App");
        using var response = await _client.SendAsync(msg);
        response.EnsureSuccessStatusCode();
        await using var data = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<Release[]>(data) ?? Array.Empty<Release>();
    }
}


public class Release
{
    /// <summary>
    /// The tag name of the release.
    /// </summary>
    [JsonPropertyName("tag_name")] public string Tag { get; set; } = "";

    /// <summary>
    /// The name of the release.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    /// <summary>
    /// The body of the release.
    /// </summary>
    [JsonPropertyName("body")] public string Body { get; set; } = "";

    /// <summary>
    /// The URL of the release, for use in browsers.
    /// </summary>
    [JsonPropertyName("html_url")] public Uri HtmlUrl { get; set; } = new("https://github.com");

    /// <summary>
    /// The assets of the release.
    /// </summary>
    [JsonPropertyName("assets")] public Asset[] Assets { get; set; } = Array.Empty<Asset>();

    /// <summary>
    /// The prerelease status of the release.
    /// </summary>
    [JsonPropertyName("prerelease")] public bool IsPrerelease { get; set; } = false;

    /// <summary>
    /// The draft status of the release.
    /// </summary>
    [JsonPropertyName("draft")] public bool IsDraft { get; set; } = false;

    /// <summary>
    /// The parsed version of the release.
    /// </summary>
    [JsonIgnore]
    public Version Version =>
        Version.TryParse(Tag.TrimStart('v'), out var version) ?
            version : Version.Parse("0.0.0.0");
}


public class Asset
{
    /// <summary>
    /// Browser download URL.
    /// </summary>
    [JsonPropertyName("browser_download_url")]
    public Uri BrowserDownloadUrl { get; set; } = new Uri("https://github.com");

    /// <summary>
    /// The name of the asset.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    /// <summary>
    /// The size of the asset.
    /// </summary>
    [JsonPropertyName("size")] public long Size { get; set; } = 0;
}
