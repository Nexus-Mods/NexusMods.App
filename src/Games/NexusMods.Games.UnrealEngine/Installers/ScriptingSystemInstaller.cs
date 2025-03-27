using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using System.Diagnostics.CodeAnalysis;
using NexusMods.Games.UnrealEngine.Models;
using IniParser;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Games.UnrealEngine.HogwartsLegacy;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class ScriptingSystemInstaller(
    ILogger<ScriptingSystemInstaller> logger,
    IConnection connection,
    IServiceProvider serviceProvider,
    IFileHashesService fileHashesService,
    TemporaryFileManager temporaryFileManager
    ) : ALibraryArchiveInstaller(serviceProvider, logger)
{
    private readonly IConnection _connection = connection;
    private readonly ILogger<ScriptingSystemInstaller> _logger = logger;
    private readonly IFileHashesService _fileHashesService = fileHashesService;
    private readonly TemporaryFileManager _temporaryFileManager = temporaryFileManager;

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var ue4ss = libraryArchive.Children.Where(x => x.Path.FileName == Constants.ScriptingSystemFileName).ToArray();
        if (ue4ss.Length == 0)
            return new NotSupported();

        var directoriesToDrop = ue4ss.FirstOrDefault().Path.Depth;

        foreach (var fileEntry in libraryArchive.Children)
        {
            var destRelativePath = (fileEntry.Path.Parts.Count() < directoriesToDrop)
                ? fileEntry.Path.DropFirst(fileEntry.Path.Depth)
                : fileEntry.Path.DropFirst(directoriesToDrop);

            var to = new GamePath(Constants.BinariesLocationId, destRelativePath);
            _ = new LoadoutFile.New(transaction, out var entityId)
            {
                Hash = fileEntry.AsLibraryFile().Hash,
                Size = fileEntry.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                {
                    TargetPath = to.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(transaction, entityId)
                    {
                        Name = fileEntry.Path.FileName,
                        LoadoutId = loadout,
                        ParentId = loadoutGroup.Id,
                    },
                },

            };
        }
        
        _ = new ScriptingSystemLoadoutItemGroup.New(transaction, loadoutGroup.Id)
        {
            IsMarker = true,
            LoadoutItemGroup = loadoutGroup,
        };

        return new Success();
    }
}
