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
using NexusMods.Games.UnrealEngine.Models;


namespace NexusMods.Games.UnrealEngine.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class UnrealEnginePakModInstaller : ALibraryArchiveInstaller
{
    private readonly IConnection _connection;
    private readonly TemporaryFileManager _temporaryFileManager;

    public UnrealEnginePakModInstaller(
        ILogger<UnrealEnginePakModInstaller> logger,
        TemporaryFileManager temporaryFileManager,
        IConnection connection,
        IServiceProvider serviceProvider) : base(serviceProvider, logger)
    {
        _temporaryFileManager = temporaryFileManager;
        _connection = connection;
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var pakEntityId = Optional<UnrealEnginePakLoadoutFileId>.None;
        foreach (var fileEntry in libraryArchive.Children)
        {
            GamePath to;
            switch (fileEntry.Path.Extension)
            {
                case var ext when Constants.ContentExts.Contains(ext):
                    to = new GamePath(Constants.PakModsLocationId, fileEntry.Path.FileName);
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
            var pakFile = new UnrealEnginePakLoadoutFile.New(transaction, entityId)
            {
                IsPakFile = true,
                LoadoutFile = loadoutFile,
            };
                
            pakEntityId = pakFile.UnrealEnginePakLoadoutFileId;
        }
        
        // TODO: UELI should be able to store multiple pak files in a single group
        _ = new UnrealEngineLoadoutItem.New(transaction, loadoutGroup.Id)
        {
            LoadoutItemGroup = loadoutGroup,
            PakMetadataId = pakEntityId.ValueOrDefault(),
        };
        

        return new Success();
    }
}
