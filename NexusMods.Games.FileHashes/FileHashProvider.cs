using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using DynamicData.Kernel;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.FileHashes.DTO;
using NexusMods.Games.FileHashes.GithubDTOs;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.FileHashes;

/// <summary>
/// Provides access to the game file hashes. Will download hashes from GitHub as needed.
/// </summary>
public class FileHashProvider
{
    private readonly HttpClient _client;
    private readonly IOSInformation _osInformation;
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    private static readonly Uri ReleaseUri = new("https://api.github.com/repos/Nexus-Mods/game-hashes/releases/latest");
    
    /// <summary>
    /// DI constructor
    /// </summary>
    public FileHashProvider(HttpClient client, IOSInformation osInformation, IFileSystem fileSystem, JsonSerializerOptions jsonSerializerOptions)
    {
        _client = client;
        _osInformation = osInformation;
        _fileSystem = fileSystem;
        _jsonSerializerOptions = jsonSerializerOptions;
        GetBaseFolder().CreateDirectory();

    }

    private AbsolutePath GetBaseFolder() 
        =>_fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory) /  
          (_osInformation.IsOSX ? "NexusMods_App/FileHashes" : "NexusMods.App/FileHashes");

    /// <summary>
    /// Get the known hashes for a given gameId. This will return all hashes for all versions. This data
    /// is not cached or stored in memory, so don't request it inside a loop or other performance critical
    /// code.
    /// </summary>
    public async Task<List<GameFileHashes>> GetHashes(GameId gameId)
    {
        var latestPath = LatestHashPath;
        if (!latestPath.HasValue)
        {
            await DownloadLatestHashesRelease();
            latestPath = LatestHashPath;
        }

        await using var archiveStream = latestPath.Value.Read();
        using var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
        
        List<GameFileHashes> entries = new();
        var gameIdString = gameId.ToString();
        foreach (var entry in zipArchive.Entries)
        {
            if (entry.Length == 0)
                continue;
            
            var relativePath = entry.FullName.ToRelativePath();
            if (relativePath.Parent.FileName != gameIdString)
                continue;
            
            await using var entryStream = entry.Open();
            var hashes = await JsonSerializer.DeserializeAsync<GameFileHashes[]>(entryStream, _jsonSerializerOptions);
            if (hashes is null)
                continue;
            
            entries.AddRange(hashes);
        }
        
        return entries;
    }

    private Optional<AbsolutePath> LatestHashPath => GetBaseFolder().EnumerateFiles()
        .OrderByDescending(f => f.FileInfo.LastWriteTime)
        .FirstOrOptional(f => true);

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
            var asset = release.Assets.FirstOrDefault(a => a.Name == "minimal_hashes.zip");
            if (asset is null)
                throw new InvalidOperationException("Failed to find the minimal_hashes.zip asset in the latest release information.");
            
            var hash = release.Name.Split(" ")[1].Trim('v');
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
