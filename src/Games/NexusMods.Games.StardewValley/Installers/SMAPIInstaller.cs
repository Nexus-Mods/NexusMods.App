using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.NexusWebApi;
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

    public SMAPIInstaller(
        IServiceProvider serviceProvider,
        ILogger<SMAPIInstaller> logger,
        IOSInformation osInformation,
        IFileHashCache fileHashCache,
        IFileOriginRegistry fileOriginRegistry)
        : base(serviceProvider)
    {
        _logger = logger;
        _osInformation = osInformation;
        _fileHashCache = fileHashCache;
        _fileOriginRegistry = fileOriginRegistry;
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
        var archiveContents = (await _fileOriginRegistry.Get(downloadId)).GetFileTree();

        // see original installer:
        // https://github.com/Pathoschild/SMAPI/blob/5919337236650c6a0d7755863d35b2923a94775c/src/SMAPI.Installer/InteractiveInstaller.cs#L384

        var isUnix = _osInformation.IsUnix();
        var isXboxGamePass = info.Store == GameStore.XboxGamePass;

        // add all files from the archive
        foreach (var kv in archiveContents.GetFiles())
        {
            var to = new GamePath(LocationId.Game, kv.Path());

            // For Linux & macOS: replace the game launcher executable "StardewValley" with "unix-launcher.sh"
            // https://github.com/Pathoschild/SMAPI/blob/5919337236650c6a0d7755863d35b2923a94775c/src/SMAPI.Installer/InteractiveInstaller.cs#L395-L425
            if (isUnix && kv.Item.FileName.Equals("unix-launcher.sh"))
            {
                to = new GamePath(LocationId.Game, "StardewValley");
            }

            // For Xbox Game Pass: replace "Stardew Valley.exe" with "StardewModdingAPI.exe"
            // https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Xbox_app
            if (isXboxGamePass && kv.Item.FileName.Equals("StardewModdingAPI.exe"))
            {
                to = new GamePath(LocationId.Game, "Stardew Valley.exe");
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

        return new [] { new ModInstallerResult
        {
            Name = "SMAPI",
            Id = info.BaseModId,
            Files = modFiles
        }};
    }
}
