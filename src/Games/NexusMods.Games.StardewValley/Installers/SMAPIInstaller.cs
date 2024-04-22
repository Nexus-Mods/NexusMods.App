using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Games.StardewValley.Models;
using NexusMods.Games.StardewValley.Sorters;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

/// <summary>
/// <see cref="IModInstaller"/> for SMAPI itself. This is different from <see cref="SMAPIModInstaller"/>,
/// which is an implementation of <see cref="IModInstaller"/> for mods that use SMAPI.
/// </summary>
public class SMAPIInstaller : AModInstaller
{
    private static readonly RelativePath InstallDatFile = "install.dat".ToRelativePath();
    private static readonly RelativePath LinuxFolder = "linux".ToRelativePath();
    private static readonly RelativePath WindowsFolder = "windows".ToRelativePath();
    private static readonly RelativePath MacOSFolder = "macOS".ToRelativePath();

    private readonly ILogger<SMAPIInstaller> _logger;
    private readonly IOSInformation _osInformation;
    private readonly IFileHashCache _fileHashCache;
    private readonly IFileOriginRegistry _fileOriginRegistry;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IFileStore _fileStore;

    public SMAPIInstaller(
        IServiceProvider serviceProvider,
        ILogger<SMAPIInstaller> logger,
        IOSInformation osInformation,
        IFileHashCache fileHashCache,
        IFileOriginRegistry fileOriginRegistry,
        TemporaryFileManager temporaryFileManager,
        IFileStore fileStore)
        : base(serviceProvider)
    {
        _logger = logger;
        _osInformation = osInformation;
        _fileHashCache = fileHashCache;
        _fileOriginRegistry = fileOriginRegistry;
        _temporaryFileManager = temporaryFileManager;
        _fileStore = fileStore;
    }

    private static KeyedBox<RelativePath, ModFileTree>[] GetInstallDataFiles(KeyedBox<RelativePath, ModFileTree> files)
    {
        var installDataFiles = files
            .GetFiles()
            .Where(kv =>
            {
                var fileName = kv.FileName();
                var parent = kv.Parent()!.Item.FileName;

                return fileName.Equals(InstallDatFile) &&
                       (parent.Equals(LinuxFolder) ||
                        parent.Equals(MacOSFolder) ||
                        parent.Equals(WindowsFolder));
            })
            .ToArray();

        return installDataFiles;
    }

    public override async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var modFiles = new List<AModFile>();

        var isSMAPI = false;
        if (info.ArchiveMetaData is NexusModsArchiveMetadata nexusMetadata)
        {
            // https://www.nexusmods.com/stardewvalley/mods/2400
            isSMAPI = nexusMetadata.GameDomain == StardewValley.GameDomain &&
                      nexusMetadata.ModId == Abstractions.NexusWebApi.Types.ModId.From(2400);
        }

        var installDataFiles = GetInstallDataFiles(info.ArchiveFiles);
        if (installDataFiles.Length != 3)
        {
            if (isSMAPI)
            {
                _logger.LogError("SMAPI doesn't contain three install.dat files, unable to install SMAPI. This might be a bug with the installer");
            }

            return NoResults;
        }

        var installDataFile = _osInformation.MatchPlatform(
            state: ref installDataFiles,
            onWindows: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("windows")),
            onLinux: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("linux")),
            onOSX: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("macOS"))
        );

        // get the archive contents of the "install.dat" file which is an archive in an archive
        var found = _fileOriginRegistry.GetByHash(installDataFile.Item.Hash).ToArray();

        DownloadId downloadId;
        if (found.Length == 0)
        {
            downloadId = await _fileOriginRegistry.RegisterDownload(
                installDataFile.Item.StreamFactory!,
                new FilePathMetadata { OriginalName = installDataFile.Item.FileName, Quality = Quality.Low },
                cancellationToken
            );
        }
        else
        {
            downloadId = found[0];
        }

        var gameFolderPath = info.Locations[LocationId.Game];
        var archiveContents = (await _fileOriginRegistry.Get(downloadId)).GetFileTree(_fileStore);

        // see original installer:
        // https://github.com/Pathoschild/SMAPI/blob/5919337236650c6a0d7755863d35b2923a94775c/src/SMAPI.Installer/InteractiveInstaller.cs#L384

        var isUnix = _osInformation.IsUnix();

        // NOTE(erri120): paths can be verified using Steam depots: https://steamdb.info/app/413150/depots/
        RelativePath unixLauncherPath = _osInformation.IsOSX
            ? "Contents/MacOS/StardewValley"
            : "StardewValley";

        string? version = null;

        // add all files from the archive
        foreach (var kv in archiveContents.GetFiles())
        {
            var to = new GamePath(LocationId.Game, kv.Path());
            var item = kv.Item;

            // NOTE(erri120): this approach is much more reliable for getting
            // the current SMAPI version than using external metadata.
            if (item.FileName.Equals("StardewModdingAPI.dll"))
            {
                try
                {
                    await using var tempFile = _temporaryFileManager.CreateFile(new Extension(".dll"));
                    await using (var stream = await item.OpenAsync())
                    {
                        await using var fs = tempFile.Path.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                        await stream.CopyToAsync(fs, cancellationToken);
                    }

                    version = tempFile.Path.FileInfo.GetFileVersionInfo().GetVersionString();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception while getting version of {Path}", item.Path);
                }
            }

            // For Linux & macOS: replace the game launcher executable "StardewValley" with "unix-launcher.sh"
            // https://github.com/Pathoschild/SMAPI/blob/5919337236650c6a0d7755863d35b2923a94775c/src/SMAPI.Installer/InteractiveInstaller.cs#L395-L425
            if (isUnix && item.FileName.Equals("unix-launcher.sh"))
            {
                to = new GamePath(LocationId.Game, unixLauncherPath);
            }

            // NOTE(erri120): The official installer doesn't replace "Stardew Valley.exe" with
            // "StardewModdingAPI.exe" to allow players to run the vanilla game without having
            // to uninstall SMAPI. However, we don't need this behavior.
            // https://github.com/Nexus-Mods/NexusMods.App/issues/1012#issuecomment-1971039971
            if (item.FileName.Equals("StardewModdingAPI.exe"))
            {
                to = new GamePath(LocationId.Game, "Stardew Valley.exe");
            }

            if (item.FileName.Equals("metadata.json")
                && item.Parent is not null
                && item.Parent.Item.FileName.Equals("smapi-internal"))
            {
                var storedFile = kv.ToStoredFile(to, [new SMAPIModDatabaseMarker()]);
                modFiles.Add(storedFile);
                continue;
            }

            modFiles.Add(kv.ToStoredFile(to));
        }

        // copy the game file "Stardew Valley.deps.json" to "StardewModdingAPI.deps.json"
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L419-L425
        var gameDepsFilePath = gameFolderPath.Combine("Stardew Valley.deps.json");

        if (_fileHashCache.TryGetCached(gameDepsFilePath, out var gameDepsFileCache))
        {
            modFiles.Add(new StoredFile
            {
                Hash = gameDepsFileCache.Hash,
                Size = gameDepsFileCache.Size,
                Id = ModFileId.NewId(),
                To = new GamePath(LocationId.Game, "StardewModdingAPI.deps.json")
            });
        }
        else
        {
            _logger.LogError("Unable to find {Path} in the game folder. Your installation might be broken!", gameDepsFilePath);
        }

        version ??= "0.0.0";

        return new[]
        {
            new ModInstallerResult
            {
                Name = "SMAPI",
                Id = info.BaseModId,
                Files = modFiles,
                SortRules = new[]
                {
                    new SMAPISorter(),
                },
                Metadata =
                [
                    new SMAPIMarker
                    {
                        Version = version,
                    },
                ],
                Version = version,
            },
        };
    }
}
