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
using NexusMods.Abstractions.IO;
using NexusMods.Games.UnrealEngine.Models;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;


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
        var luaFiles = libraryArchive.Children.Where(IsMainLua).ToList();
        if (!luaFiles.Any())
            return new NotSupported();
        
        var luaFileMappings = luaFiles.ToDictionary(
            luaFile => luaFile.Path.Parent.Parent,
            luaFile => libraryArchive.Children
                .Where(child => child.Path.InFolder(luaFile.Path.Parent.Parent))
                .ToList()
        );

        foreach (var kvp in luaFileMappings)
        {
            var (key, luaPath) = kvp;
            foreach (var fileEntry in luaPath)
            {
                var to = new GamePath(Constants.LuaModsLocationId, fileEntry.Path.RelativeTo(key.Parent));
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

                if (IsMainLua(fileEntry))
                {
                    _ = new ScriptingSystemLuaLoadoutItem.New(transaction, loadoutGroup)
                    {
                        LoadoutItemGroup = loadoutGroup,
                        LoadOrderName = key.Parts.Last().ToString(),
                    };
                }
            }
        }

        return new Success();
    }
    
    private bool IsMainLua(LibraryArchiveFileEntry.ReadOnly libraryFile) =>
        libraryFile.Path.FileName.Equals("main.lua");
}
