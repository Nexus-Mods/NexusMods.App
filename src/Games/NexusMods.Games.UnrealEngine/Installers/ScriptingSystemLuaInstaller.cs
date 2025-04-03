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
using NexusMods.Games.UnrealEngine.Models;


namespace NexusMods.Games.UnrealEngine.Installers;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "IdentifierTypo")]
public class ScriptingSystemLuaInstaller(
    ILogger<ScriptingSystemLuaInstaller> logger,
    IConnection connection,
    IServiceProvider serviceProvider
    ) : ALibraryArchiveInstaller(serviceProvider, logger)
{
    private readonly IConnection _connection = connection;

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var luaFiles = libraryArchive.Children.Where(x => x.Path.Extension == Constants.LuaExt).ToArray();
        if (luaFiles.Length == 0)
            return new NotSupported();
        
        _ = new ScriptingSystemLuaLoadoutItem.New(transaction, loadoutGroup.Id) { LoadoutItemGroup = loadoutGroup,  };

        foreach (var fileEntry in libraryArchive.Children)
        {
            if (!luaFiles.Any(x => fileEntry.Path.InFolder(x.Path.Parent)))
                continue;
            
            var to = new GamePath(Constants.LuaModsLocationId, fileEntry.Path.DropFirst(fileEntry.Path.Depth - 1));
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
                        LoadoutId = loadout.LoadoutId,
                    },
                },
                
            };
        }

        return new Success();
    }
}
