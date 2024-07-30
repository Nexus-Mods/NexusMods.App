using DynamicData.Kernel;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using File = NexusMods.Abstractions.Loadouts.Files.File;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Installers;

public class SMAPIInstaller : ALibraryArchiveInstaller, IModInstaller
{
    private static readonly RelativePath InstallDatFile = "install.dat".ToRelativePath();
    private static readonly RelativePath LinuxFolder = "linux".ToRelativePath();
    private static readonly RelativePath WindowsFolder = "windows".ToRelativePath();
    private static readonly RelativePath MacOSFolder = "macOS".ToRelativePath();

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
        : base(serviceProvider, logger)
    {
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

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var modFiles = new List<TempEntity>();

        var isSMAPI = false;
        if (info.Source.Contains(NexusModsArchiveMetadata.GameId))
        {
            // https://www.nexusmods.com/stardewvalley/mods/2400
            var source = info.Source;
            isSMAPI = NexusModsArchiveMetadata.GameId.Get(source) == StardewValley.GameDomain &&
                      NexusModsArchiveMetadata.ModId.Get(source) == Abstractions.NexusWebApi.Types.ModId.From(2400);
        }

        var installDataFiles = GetInstallDataFiles(info.ArchiveFiles);
        if (installDataFiles.Length != 3)
        {
            if (isSMAPI)
            {
                Logger.LogError("SMAPI doesn't contain three install.dat files, unable to install SMAPI. This might be a bug with the installer");
            }

            return [];
        }

        var installDataFile = _osInformation.MatchPlatform(
            state: ref installDataFiles,
            onWindows: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("windows")),
            onLinux: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("linux")),
            onOSX: (ref KeyedBox<RelativePath, ModFileTree>[] dataFiles) => dataFiles.First(kv => kv.Path().Parent.FileName.Equals("macOS"))
        );

        // get the archive contents of the "install.dat" file which is an archive in an archive
        var found = _fileOriginRegistry.GetBy(installDataFile.Item.Hash).ToArray();

        DownloadId downloadId;
        if (found.Length == 0)
        {
            downloadId = await _fileOriginRegistry.RegisterDownload(
                installDataFile.Item.StreamFactory!,
                (tx, id) => 
                    tx.Add(id, FilePathMetadata.OriginalName, installDataFile.Item.FileName),
                "",
                cancellationToken);
        }
        else
        {
            downloadId = DownloadId.From(found[0].Id);
        }

        var gameFolderPath = info.Locations[LocationId.Game];
        var archiveContents = _fileOriginRegistry.Get(downloadId).GetFileTree(_fileStore);

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

                    var fvi = tempFile.Path.FileInfo.GetFileVersionInfo();
                    version = fvi.FileVersion.ToString(fieldCount: 3);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception while getting version of {Path}", item.Path);
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
                var storedFile = kv.ToStoredFile(to, new TempEntity
                {
                    {SMAPIModDatabaseMarker.SMAPIModDatabase, Null.Instance},
                });
                
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
            modFiles.Add(new TempEntity()
            {
                {StoredFile.Hash, gameDepsFileCache.Hash},
                {StoredFile.Size, gameDepsFileCache.Size},
                {File.To, new GamePath(LocationId.Game, "StardewModdingAPI.deps.json")},
            });
        }
        else
        {
            Logger.LogError("Unable to find {Path} in the game folder. Your installation might be broken!", gameDepsFilePath);
        }

        version ??= "0.0.0";

        return new[]
        {
            new ModInstallerResult
            {
                Name = "SMAPI",
                Id = info.BaseModId,
                Files = modFiles,
                Metadata = new TempEntity
                {
                    {SMAPIMarker.Version, version},
                },
                Version = version,
            },
        };
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var targetParentName = _osInformation.MatchPlatform(
            onWindows: static () => WindowsFolder,
            onLinux: static () => LinuxFolder,
            onOSX: static () => MacOSFolder
        );

        var foundInstallDataFile = libraryArchive.Children.TryGetFirst(fileEntry =>
        {
            var path = fileEntry.Path;
            var fileName = path.FileName;
            if (!fileName.Equals(InstallDatFile)) return false;
            var parentName = path.Parent.FileName;
            return parentName.Equals(targetParentName);
        }, out var installDataFile);

        if (!foundInstallDataFile) return new NotSupported();
        if (!installDataFile.AsLibraryFile().TryGetAsLibraryArchive(out var installDataArchive))
        {
            Logger.LogError("Expected Library Item `{LibraryItem}` (`{Id}`) to be an archive", installDataFile.AsLibraryFile().AsLibraryItem().Name, installDataFile.Id);
            return new NotSupported();
        }

        var isUnix = _osInformation.IsUnix();

        // NOTE(erri120): paths can be verified using Steam depots: https://steamdb.info/app/413150/depots/
        RelativePath unixLauncherPath = _osInformation.IsOSX
            ? "Contents/MacOS/StardewValley"
            : "StardewValley";

        // TODO: set group name
        var modDatabaseEntityId = DynamicData.Kernel.Optional<EntityId>.None;
        var version = DynamicData.Kernel.Optional<string>.None;

        foreach (var fileEntry in installDataArchive.Children)
        {
            var to = new GamePath(LocationId.Game, fileEntry.Path);
            var fileName = fileEntry.Path.FileName;

            var entityId = transaction.TempId();
            var loadoutItem = new LoadoutItem.New(transaction, entityId)
            {
                Name = fileName,
                LoadoutId = loadout,
                ParentId = loadoutGroup,
            };

            // NOTE(erri120): This is a more reliable approach for getting
            // the SMAPI version.
            if (!version.HasValue && fileName.Equals("StardewModdingAPI.dll"))
            {
                try
                {
                    await using var tempFile = _temporaryFileManager.CreateFile();
                    await using (var stream = await _fileStore.GetFileStream(fileEntry.AsLibraryFile().Hash, token: cancellationToken))
                    {
                        await using var fs = tempFile.Path.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                        await stream.CopyToAsync(fs, cancellationToken);
                    }

                    var fvi = tempFile.Path.FileInfo.GetFileVersionInfo();
                    version = fvi.FileVersion.ToString(fieldCount: 3);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception while getting version of SMAPI from DLL");
                }
            }

            // For Linux & macOS: replace the game launcher executable "StardewValley" with "unix-launcher.sh"
            // https://github.com/Pathoschild/SMAPI/blob/5919337236650c6a0d7755863d35b2923a94775c/src/SMAPI.Installer/InteractiveInstaller.cs#L395-L425
            if (isUnix && fileName.Equals("unix-launcher.sh"))
            {
                to = new GamePath(LocationId.Game, unixLauncherPath);
            }

            // NOTE(erri120): The official installer doesn't replace "Stardew Valley.exe" with
            // "StardewModdingAPI.exe" to allow players to run the vanilla game without having
            // to uninstall SMAPI. However, we don't need this behavior.
            // https://github.com/Nexus-Mods/NexusMods.App/issues/1012#issuecomment-1971039971
            if (!isUnix && fileName.Equals("StardewModdingAPI.exe"))
            {
                to = new GamePath(LocationId.Game, "Stardew Valley.exe");
            }

            var loadoutFile = new LoadoutFile.New(transaction, entityId)
            {
                Hash = fileEntry.AsLibraryFile().Hash,
                Size = fileEntry.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                {
                    TargetPath = to,
                    LoadoutItem = loadoutItem,
                },
            };

            if (!modDatabaseEntityId.HasValue &&
                fileName.Equals("metadata.json") &&
                fileEntry.Path.Parent.FileName.Equals("smapi-internal"))
            {
                _ = new SMAPIModDatabaseLoadoutFile.New(transaction, entityId)
                {
                    IsIsModDatabaseFileMarker = true,
                    LoadoutFile = loadoutFile,
                };

                modDatabaseEntityId = entityId;
            }
        }

        // copy the game file "Stardew Valley.deps.json" to "StardewModdingAPI.deps.json"
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L419-L425
        var foundGameFilesGroup = LoadoutGameFilesGroup
            .FindByGameMetadata(loadout.Db, loadout.Installation.GameMetadataId)
            .TryGetFirst(x => x.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId, out var gameFilesGroup);

        if (!foundGameFilesGroup)
        {
            Logger.LogError("Unable to find game files group!");
        }
        else
        {
            var targetPath = new GamePath(LocationId.Game, "Stardew Valley.deps.json");
            var foundGameDepsFile = gameFilesGroup.AsLoadoutItemGroup().Children
                .TryGetFirst(gameFile => gameFile.TryGetAsLoadoutItemWithTargetPath(out var targeted) && targeted.TargetPath == targetPath,
                    out var gameDepsFile);
            if (!foundGameDepsFile)
            {
                Logger.LogError("Unable to find `{Path}` in game files group!", targetPath);
            }
            else
            {
                var gameFile = LoadoutFile.Load(gameDepsFile.Db, gameDepsFile.Id);
                
                var to = new GamePath(LocationId.Game, "StardewModdingAPI.deps.json");
                _ = new LoadoutFile.New(transaction, out var id)
                {
                    Hash = gameFile.Hash,
                    Size = gameFile.Size,
                    LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                    {
                        TargetPath = to,
                        LoadoutItem = new LoadoutItem.New(transaction, id)
                        {
                            Name = to.FileName,
                            LoadoutId = loadout,
                            ParentId = loadoutGroup,
                        },
                    },
                };
            }
        }

        _ = new SMAPILoadoutItem.New(transaction, loadoutGroup.Id)
        {
            LoadoutItemGroup = loadoutGroup,
            Version = version.ValueOrDefault(),
            ModDatabaseId = modDatabaseEntityId.ValueOrDefault(),
        };

        return new Success();
    }
}
