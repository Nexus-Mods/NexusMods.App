using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Common.OSInterop;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Overlays.Updater;

public class UpdaterViewModel : AViewModel<IUpdaterViewModel>, IUpdaterViewModel
{
    private Uri _githubRepo = new("https://api.github.com/repos/Nexus-Mods/NexusMods.App/releases");

    private readonly HttpClient _client;
    private readonly ILogger<UpdaterViewModel> _logger;
    public bool IsActive { get; set; }
    public InstallationMethod Method { get; }
    public Version NewVersion { get; } = Version.Parse("0.0.0.0");
    public Version OldVersion { get; }
    public ICommand UpdateCommand { get; }

    public Uri UpdateUrl { get; set; } = new("https://github.com/Nexus-Mods/NexusMods.App/releases/latest");

    [Reactive] public bool ShowSystemUpdateMessage { get; set; } = false;

    public UpdaterViewModel(ILogger<UpdaterViewModel> logger, IOSInterop interop, HttpClient client)
    {
        _client = client;
        _logger = logger;
        OldVersion = ApplicationConstants.CurrentVersion;
        Method = CompileConstants.InstallationMethod;

        UpdateCommand = ReactiveCommand.Create(() =>
        {
            IsActive = false;
            interop.OpenUrl(UpdateUrl);
        });
    }

    public async Task<bool> ShouldShow()
    {
        _logger.LogInformation("Checking for updates from GitHub");
        try
        {
            var releases = await GetReleases();

            return true;
        }
        catch (Exception ex)
        {

            return false;
        }

    }

    private async Task<Release[]> GetReleases()
    {
        await using var data = await _client.GetStreamAsync(_githubRepo);
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
    /// The assets of the release.
    /// </summary>
    [JsonPropertyName("assets")] public Asset[] Assets { get; set; } = Array.Empty<Asset>();
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
