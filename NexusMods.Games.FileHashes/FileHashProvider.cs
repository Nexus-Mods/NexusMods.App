using System.Globalization;
using System.Net.Http.Json;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.FileHashes.GithubDTOs;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes;

public class FileHashProvider
{
    private readonly ISettingsManager _settingsManager;
    private readonly HttpClient _client;
    private readonly IOSInformation _osInformation;
    private readonly IFileSystem _fileSystem;

    private static readonly Uri ReleaseUri = new("https://api.github.com/repos/Nexus-Mods/game-hashes/releases/latest");
    
    
    public FileHashProvider(ISettingsManager settingsManager, HttpClient client, IOSInformation osInformation, IFileSystem fileSystem)
    {
        _settingsManager = settingsManager;
        _client = client;
        _osInformation = osInformation;
        _fileSystem = fileSystem;
        GetBaseFolder().CreateDirectory();

    }

    private AbsolutePath GetBaseFolder() 
        =>_fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory) /  
          (_osInformation.IsOSX ? "NexusMods_App/FileHashes" : "NexusMods.App/FileHashes");

    /// <summary>
    /// Attempt to get the most recent game hash release version from GitHub.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<(Hash Hash, Uri DownloadUri)> GetCurrentGithubVersion()
    {
        var release = await _client.GetFromJsonAsync<Release>(ReleaseUri);
        if (release is null)
            throw new InvalidOperationException("Failed to get the latest release information from GitHub.");
        try
        {
            var asset = release.Assets.FirstOrDefault(a => a.Name == "minimal_hashes.nx");
            if (asset is null)
                throw new InvalidOperationException("Failed to find the minimal_hashes.nx asset in the latest release information.");
            
            var hash = release.Name.Split(" ")[1];
            if (ulong.TryParse(hash, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
                return (Hash.From(result), asset.BrowserDownloadUrl);
            throw new InvalidOperationException("Failed to parse the hash from the latest release information.");
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to parse the hash from the latest release information.", e);
        }
    }

    /// <summary>
    /// Download the latest game hashes release from GitHub.
    /// </summary>
    public async Task<(Hash Hash, AbsolutePath Path)> DownloadLatestHashesRelease()
    {
        var (latestHash, uri) = await GetCurrentGithubVersion();
        var destination = GetBaseFolder() / (latestHash + ".nx");
        if (_fileSystem.FileExists(destination))
            return (latestHash, destination);
        
        var tmpPath = GetBaseFolder() / (Guid.NewGuid() + ".tmp");

        {
            await using var stream = await _client.GetStreamAsync(uri);
            await using var fileStream = _fileSystem.CreateFile(tmpPath);
            await stream.CopyToAsync(fileStream);
        }

        var downloadedHash = await tmpPath.XxHash3Async();
        if (downloadedHash != latestHash)
            throw new InvalidOperationException("The downloaded file hash does not match the expected hash.");
        
        await tmpPath.MoveToAsync(destination);
        return (latestHash, destination);
    }
}
