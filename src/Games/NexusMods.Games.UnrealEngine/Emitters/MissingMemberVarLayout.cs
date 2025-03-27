using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.UnrealEngine;
using NexusMods.Games.UnrealEngine.Avowed;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Games.UnrealEngine.Models;
using NexusMods.Paths;
using Diagnostic = NexusMods.Abstractions.Diagnostics.Diagnostic;

namespace NexusMods.Games.UnrealEngine.Emitters;

public class MissingMemberVarLayout : ILoadoutDiagnosticEmitter
{
    private static readonly string MemberVariableFileName = "MemberVariableLayout.ini";

    private readonly IGameRegistry _gameRegistry;
    private readonly IFileStore _fs;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly ILogger _logger;

    public MissingMemberVarLayout(
        ILogger<MissingMemberVarLayout> logger,
        IGameRegistry gameRegistry,
        IFileStore fileStore,
        TemporaryFileManager temporaryFileManager)
    {
        _gameRegistry = gameRegistry;
        _logger = logger;
        _fs = fileStore;
        _temporaryFileManager = temporaryFileManager;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        var game = _gameRegistry.InstalledGames
            .Where(x => x.Game.GameId == loadout.Installation.GameId)
            .Select(x => x.GetGame());
        var ueAddon = game
            .Cast<IUnrealEngineGameAddon>()
            .FirstOrDefault();
        if (ueAddon is null || ueAddon.GetMemberVariableTemplate == null)
        {
            yield break;
        }

        if (!Utils.TryGetScriptingSystemLoadoutGroup(loadout, true, out var ue4ssLoadoutItems))
        {
            yield break;
        }
        
        var loadoutWithTxId = loadout.GetLoadoutWithTxId();
        var memberVariableFile = ue4ssLoadoutItems.First().AsLoadoutItemGroup().Children
            .Select(gameFile => gameFile.TryGetAsLoadoutItemWithTargetPath(out var targeted)
                ? (GamePath)targeted.TargetPath
                : default(GamePath))
            .FirstOrDefault(x => x != default(GamePath) && x.FileName == MemberVariableFileName);
        // if (memberVariableFile == null)
        // {
        //     var memberVariableTemplate = ueAddon.GetMemberVariableTemplate.GetStreamAsync().GetAwaiter().GetResult();
        //     _temporaryFileManager.CreateFile()
        //     var destination = new GamePath(Constants.BinariesLocationId, $"ue4ss/{MemberVariableFileName}");
        //     _ = new LoadoutFile.New(loadoutWithTxId.Tx, out var newLoadoutFile)
        //     {
        //         Hash = memberVariableTemplate.Hash,
        //         Size = memberVariableTemplate.Size,
        //         LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New()
        //         {
        //             TargetPath = destination.ToGamePathParentTuple(withTxId.LoadoutId),
        //             LoadoutItem = new LoadoutItem.New()
        //             {
        //                 Name = MemberVariableFileName,
        //                 LoadoutId = withTxId.LoadoutId,
        //                 ParentId = ue4ssLoadoutItems.First().AsLoadoutItemGroup().Id
        //             }
        //         }
        //     };
        // }
    }

    // public async Task FixAsync(
    //     Loadout.ReadOnly loadout,
    //     [EnumeratorCancellation] CancellationToken cancellationToken)
    // {
    //     await Task.Yield();
    //
    //     var game = _gameRegistry.InstalledGames
    //         .Where(x => x.Game.GameId == loadout.Installation.GameId)
    //         .Select(x => x.GetGame());
    //     var ueAddon = game
    //         .Cast<IUnrealEngineGameAddon>()
    //         .FirstOrDefault();
    //     if (ueAddon is null || ueAddon.GetMemberVariableTemplate == null)
    //     {
    //         yield break;
    //     }
    //
    //     if (!Utils.TryGetScriptingSystemLoadoutGroup(loadout, true, out var ue4ssLoadoutItems))
    //     {
    //         return false;
    //     }
    //
    //     var memberVariableTemplate = ueAddon.GetMemberVariableTemplate.GetStreamAsync().GetAwaiter().GetResult();
    //     var memberVariablePath = new GamePath(Constants.BinariesLocationId, $"ue4ss/{MemberVariableFileName}");
    //
    //     // Add the new LoadoutFile entry to the LoadoutGroup
    //     var loadoutGroup = ue4ssLoadoutItems.First().AsLoadoutItemGroup();
    //     loadoutGroup.(newLoadoutFile);
    //     return true;
    // }
}
