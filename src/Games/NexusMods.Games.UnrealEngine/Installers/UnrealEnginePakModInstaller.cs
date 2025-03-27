using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Games.UnrealEngine.Models;

namespace NexusMods.Games.UnrealEngine.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class UnrealEnginePakModInstaller(
    ILogger<UnrealEnginePakModInstaller> logger,
    IFileStore fileStore,
    TemporaryFileManager temporaryFileManager,
    IConnection connection,
    IGameRegistry gameRegistry,
    IServiceProvider serviceProvider)
    : ALibraryArchiveInstaller(serviceProvider, logger)
{
    private readonly IConnection _connection = connection;
    private readonly ILogger<UnrealEnginePakModInstaller> _logger = logger;
    private readonly IGameRegistry _gameRegistry = gameRegistry;
    private readonly IFileStore _fileStore = fileStore;
    private readonly TemporaryFileManager _temporaryFileManager = temporaryFileManager;

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        if (!IsSupported(libraryArchive))
            return new NotSupported();
        
        var ueGameAddon = _gameRegistry.InstalledGames
            .Where(game => game.Game.GameId == loadout.Installation.GameId)
            .Select(game => game.GetGame())
            .Cast<IUnrealEngineGameAddon>()
            .FirstOrDefault();

        if (ueGameAddon == null)
            return new NotSupported();
        
        var pakMetadataDict = await GetPakMetadata(loadout, ueGameAddon, libraryArchive, cancellationToken);
        foreach (var fileEntry in libraryArchive.Children)
        {
            GamePath to;
            switch (fileEntry.Path.Extension)
            {
                case var ext when Constants.ContentExts.Contains(ext):
                    var key = Path.GetFileNameWithoutExtension(fileEntry.Path.FileName);
                    to = new GamePath(pakMetadataDict[key].MountPoint, fileEntry.Path.FileName);
                    break;
                case var ext when ext == Constants.DllExt:
                    to = new GamePath(Constants.BinariesLocationId, fileEntry.Path.FileName);
                    break;
                case var ext when ext == Constants.SaveExt:
                    to = new GamePath(LocationId.Saves, fileEntry.Path.FileName);
                    break;
                case var ext when ext == Constants.ConfigExt:
                    to = new GamePath(Constants.ConfigLocationId, fileEntry.Path.FileName);
                    break;
                case var ext when ext == Constants.LuaExt:
                    var relPath = (fileEntry.Path.Depth != 2)
                        ? fileEntry.Path.DropFirst(fileEntry.Path.Depth - 1)
                        : fileEntry.Path;
                    to = new GamePath(Constants.LuaModsLocationId, relPath);
                    break;
                default:
                    Logger.LogWarning("File {FileName} is of unrecognized type {Extension} - skipping", fileEntry.Path.FileName, fileEntry.Path.Extension);
                    continue;
            }
            
            var loadoutFile = new LoadoutFile.New(transaction, out var entityId)
            {
                Hash = fileEntry.AsLibraryFile().Hash,
                Size = fileEntry.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                {
                    TargetPath = to.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(transaction, entityId)
                    {
                        Name = fileEntry.AsLibraryFile().FileName,
                        LoadoutId = loadout,
                        ParentId = loadoutGroup,
                    },
                },
            };

            if (fileEntry.Path.Extension != Constants.PakExt) continue;
            _ = new UnrealEnginePakLoadoutFile.New(transaction, entityId)
            {
                IsPakFile = true,
                LoadoutFile = loadoutFile,
            };
        }
        
        _ = new UnrealEngineLoadoutItem.New(transaction, loadoutGroup.Id)
        {
            LoadoutItemGroup = loadoutGroup,
        };
        
        return new Success();
    }

    private bool IsSupported(LibraryArchive.ReadOnly libraryArchive)
    {
        var pakFiles = libraryArchive.Children.Where(x => x.Path.Extension == Constants.PakExt);
        var pakDirectories = pakFiles.Select(x => x.Path.Parent).Distinct().ToList();
        return pakDirectories.Count == 1;
    }
    
    private async ValueTask<Dictionary<string, PakMetaData>> GetPakMetadata(
        Loadout.ReadOnly loadout,
        IUnrealEngineGameAddon ueGameAddon,
        LibraryArchive.ReadOnly libraryArchive,
        CancellationToken cancellationToken)
    {
        var pakFiles = libraryArchive.Children.Where(x => x.Path.Extension == Constants.PakExt);
        var results = new Dictionary<string, PakMetaData>();

        foreach (var fileEntry in pakFiles)
        {
            var key = Path.GetFileNameWithoutExtension(fileEntry.Path.FileName);
            try
            {
                var pakMetadata = await PakFileParser.Deserialize(
                    ueGameAddon,
                    _temporaryFileManager,
                    _fileStore,
                    fileEntry.AsLibraryFile(),
                    cancellationToken);

                results.Add(key, pakMetadata);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while deserializing {Path} from {Archive}", fileEntry.Path, fileEntry.Parent.AsLibraryFile().FileName);
            }
        }

        return results;
    }
}
