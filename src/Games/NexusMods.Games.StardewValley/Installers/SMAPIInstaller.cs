using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.StardewValley.Installers;

public class SMAPIInstaller : ALibraryArchiveInstaller
{
    private static readonly RelativePath InstallDatFile = "install.dat".ToRelativePath();
    private static readonly RelativePath LinuxFolder = "linux".ToRelativePath();
    private static readonly RelativePath WindowsFolder = "windows".ToRelativePath();
    private static readonly RelativePath MacOSFolder = "macOS".ToRelativePath();

    private readonly IOSInformation _osInformation;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IFileStore _fileStore;
    private readonly IFileHashesService _fileHashesService;

    public SMAPIInstaller(
        IServiceProvider serviceProvider,
        ILogger<SMAPIInstaller> logger,
        IOSInformation osInformation,
        TemporaryFileManager temporaryFileManager,
        IFileHashesService fileHashesService,
        IFileStore fileStore)
        : base(serviceProvider, logger)
    {
        _osInformation = osInformation;
        _temporaryFileManager = temporaryFileManager;
        _fileStore = fileStore;
        _fileHashesService = fileHashesService;
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

        var modDatabaseEntityId = Optional<EntityId>.None;
        var version = Optional<string>.None;

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
                    TargetPath = to.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = loadoutItem,
                },
            };

            if (!modDatabaseEntityId.HasValue &&
                fileName.Equals("metadata.json") &&
                fileEntry.Path.Parent.FileName.Equals("smapi-internal"))
            {
                _ = new SMAPIModDatabaseLoadoutFile.New(transaction, entityId)
                {
                    IsModDatabaseFile = true,
                    LoadoutFile = loadoutFile,
                };

                modDatabaseEntityId = entityId;
            }
        }

        // copy the game file "Stardew Valley.deps.json" to "StardewModdingAPI.deps.json"
        // https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L419-L425
        var srcPath = new GamePath(LocationId.Game, "Stardew Valley.deps.json");
        await _fileHashesService.GetFileHashesDb();
        var foundRecord = _fileHashesService.GetGameFiles((loadout.InstallationInstance.Store, loadout.LocatorIds.ToArray()))
            .Where(f => f.Path == srcPath)
            .TryGetFirst(out var gameFileRecord);

        if (!foundRecord)
        {
            Logger.LogError("Can't find the game file record for `{Path}`", srcPath);
        }
        else
        {
            var to = new GamePath(LocationId.Game, "StardewModdingAPI.deps.json");
            _ = new LoadoutFile.New(transaction, out var id)
            {
                Hash = gameFileRecord.Hash,
                Size = gameFileRecord.Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, id)
                {
                    TargetPath = to.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(transaction, id)
                    {
                        Name = to.FileName,
                        LoadoutId = loadout,
                        ParentId = loadoutGroup,
                    },
                },
            };
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
