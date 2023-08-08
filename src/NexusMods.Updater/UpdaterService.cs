using System.Diagnostics;
using System.IO.Compression;
using CliWrap;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using NexusMods.Updater.DownloadSources;
using NexusMods.Updater.DTOs;

namespace NexusMods.Updater;

public class UpdaterService
{
    private readonly ILogger<UpdaterService> _logger;
    private readonly IFileSystem _fileSystem;
    private Task? _runnerTask;
    private readonly AbsolutePath _updateFolder;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _token;
    private readonly Github _github;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IHttpDownloader _downloader;
    private readonly IProcessFactory _processFactory;
    private readonly AbsolutePath _appFolder;

    public UpdaterService(ILogger<UpdaterService> logger, IFileSystem fileSystem, Github github,
        TemporaryFileManager temporaryFileManager, IHttpDownloader downloader, IProcessFactory processFactory)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _appFolder = fileSystem.GetKnownPath(KnownPath.EntryDirectory);
        _updateFolder = _appFolder.Combine(Constants.UpdateFolder);
        _github = github;
        _cancellationTokenSource = new CancellationTokenSource();
        _token = _cancellationTokenSource.Token;
        _temporaryFileManager = temporaryFileManager;
        _downloader = downloader;
        _processFactory = processFactory;
    }

    public bool IsOnlyInstance()
    {
        var thisName = Process.GetCurrentProcess().ProcessName;
        return Process.GetProcesses().Count(p => p.ProcessName == thisName) > 1;
    }

    public async Task<bool> IsUpdateReady()
    {
        var updateMarkerFile = _fileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine(Constants.UpdateFolder)
            .Combine(Constants.UpdateMarkerFile);

        if (updateMarkerFile.FileExists)
            return true;

        if (_updateFolder.DirectoryExists())
        {
            _logger.LogDebug($"Old update folder exists, deleting...");
            _updateFolder.DeleteDirectory();
        }

        return false;
    }


    public async Task Startup()
    {
        if (await IsUpdateReady())
        {
            await RunUpdate();
        }
#if DEBUG
        _runnerTask = Task.Run(async () => await RunLoop(), _token);
#endif
    }

    private async Task RunUpdate()
    {
        var updateProgram = _updateFolder.Combine(Constants.UpdateExecutable);

        var cmd = new Command(updateProgram.ToString())
            .WithWorkingDirectory(_updateFolder.Combine(Constants.UpdateExecutable).ToString())
            .WithArguments(new[]
            {
                "copy-to-app-folder",
                "-f", _updateFolder.ToString(),
                "-t", _appFolder.ToString(),
                "-p", Environment.ProcessId.ToString(),
                "-c", _appFolder.Combine(Constants.UpdateExecutable).ToString()
            });

        var info = new ProcessStartInfo(cmd.TargetFilePath)
        {
            Arguments = cmd.Arguments,
            WorkingDirectory = cmd.WorkingDirPath,

        };
        var process = Process.Start(info);
        if (process == null)
        {
            _logger.LogError("Failed to start update process");
            return;
        }
        Environment.Exit(0);
    }

    private async Task RunLoop()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var currentVersionString = Process.GetCurrentProcess().MainModule?.FileVersionInfo?.ProductVersion;
            if (!Version.TryParse(currentVersionString, out var version))
                version = new Version(0, 0, 0, 1);

            var latestRelease = await _github.GetLatestRelease("Nexus-Mods", "NexusMods.App");
            if (latestRelease != null)
            {
                if (Version.TryParse(latestRelease.Tag.TrimStart('v'), out var latestVersion))
                {
                    if (latestVersion > version)
                    {
                        _logger.LogInformation("New version available: {LatestVersion}", latestVersion);
                        await using var file = await DownloadRelease(latestRelease);
                        if (file != null)
                        {
                            await ExtractRelease(file.Value.Path, latestVersion);
                        }


                    }
                }
            }
            await Task.Delay(TimeSpan.FromHours(6), _token);
        }
    }

    private async Task ExtractRelease(AbsolutePath file, Version newVersion)
    {
        var destination = _fileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine(Constants.UpdateFolder);
        if (destination.DirectoryExists())
        {
            _logger.LogDebug($"Old update folder exists, deleting...");
            destination.DeleteDirectory();
        }

        _logger.LogInformation("Extracting new version ({Version}) of the app", newVersion);
        using var archive = new ZipArchive(file.Read(), ZipArchiveMode.Read, false);
        _updateFolder.CreateDirectory();

        foreach (var entry in archive.Entries)
        {
            var destinationPath = destination.Combine(entry.FullName);
            destinationPath.Parent.CreateDirectory();
            if (entry.FullName.EndsWith("/"))
                continue;

            await using var stream = entry.Open();
            await using var destinationStream = destinationPath.Create();
            await stream.CopyToAsync(destinationStream, _token);
        }

        await destination.Combine(Constants.UpdateMarkerFile)
            .WriteAllTextAsync(newVersion.ToString(), _token);
        _logger.LogInformation("Update marker file created for version {Version}", newVersion);
    }

    private async Task<TemporaryPath?> DownloadRelease(Release latestRelease)
    {
        var os = "";
        if (OSInformation.Shared.IsWindows)
        {
            os = "win";
        }
        else if (OSInformation.Shared.IsLinux)
        {
            os = "linux";
        }
        var asset = latestRelease.Assets.FirstOrDefault(a => a.Name.StartsWith("NexusMods.App-") && a.Name.EndsWith($"{os}-x64.zip"));
        if (asset == null) return null;

        _logger.LogInformation("Downloading new version of the app {AssetName}", asset.Name);
        var destination = _temporaryFileManager.CreateFile();
        await _downloader.DownloadAsync(new[] {new HttpRequestMessage(HttpMethod.Get, asset.BrowserDownloadUrl)} , destination.Path, null, null, _token);

        return destination;
    }
}
