using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Resources;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Games.UnrealEngine.Models;
using Polly;

using Diagnostic = NexusMods.Abstractions.Diagnostics.Diagnostic;

namespace NexusMods.Games.UnrealEngine.Emitters;

public class AssetConflictDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly ILogger _logger;
    private readonly IFileStore _fileStore;
    private readonly IGameRegistry _gameRegistry;
    private readonly IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<PakMetaData>> _metadataPipeline;

    public AssetConflictDiagnosticEmitter(
        ILogger<AssetConflictDiagnosticEmitter> logger,
        IServiceProvider serviceProvider,
        IGameRegistry gameRegistry,
        IFileStore fileStore)
    {
        _gameRegistry = gameRegistry;
        _logger = logger;
        _fileStore = fileStore;
        _metadataPipeline = Pipelines.GetMetadataPipeline(serviceProvider);
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var ueAddon = _gameRegistry.InstalledGames
            .Where(x => x.Game.GameId == loadout.Installation.GameId)
            .Select(x => x.GetGame())
            .Cast<IUnrealEngineGameAddon>()
            .FirstOrDefault();
        if (ueAddon is null)
        {
            _logger.LogWarning("UE Game addon not found in registry: {GameId}", loadout.Installation.GameId);
            yield break;
        }
        var pakFiles = Utils.GetAllLoadoutFilesWithExt(loadout, [Constants.PakModsLocationId], [Constants.PakExt], true);
        
        var fileTasks = await pakFiles.ToAsyncEnumerable()
        .Select(async file =>
        {
            file.TryGetAsUnrealEnginePakLoadoutFile(out var pakFile);
            var meta = await _metadataPipeline.LoadResourceAsync(pakFile, cancellationToken);
            var assets = meta.Data.Result.PakAssets;
            return assets.Select(file => new
            {
                AssetName = file.Name,
                ModFile = pakFile,
            }).ToList();
        })
        .ToListAsync(cancellationToken: cancellationToken);

        var ueAssetLookup = fileTasks
            .SelectMany(task => task.Result)
            .ToLookup(x => x.AssetName, x => x.ModFile);

        var diagnostics = ueAssetLookup
            .Where(x => x.Count() > 1)
            .Select(x => Diagnostics.CreateUnrealEngineAssetConflict(
                ConflictingItems: string.Join(", ", x.ToArray().Select(x => x.AsLoadoutFile().AsLoadoutItemWithTargetPath().AsLoadoutItem().Name)),
                ModifiedUEAsset: x.Key
            ));

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }
}
