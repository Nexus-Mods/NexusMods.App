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
    private readonly IResourceLoader<UnrealEnginePakLoadoutFile.ReadOnly, Outcome<Dictionary<string, PakMetaData>>> _metadataPipeline;

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
        
        var pakFiles = Utils.GetAllLoadoutFilesWithExt(loadout, [Constants.PakModsLocationId], [Constants.PakExt], true);
        var fileTasks = await pakFiles.ToAsyncEnumerable()
        .Select(async file =>
        {
            file.TryGetAsUnrealEnginePakLoadoutFile(out var pakFile);
            var meta = await _metadataPipeline.LoadResourceAsync(pakFile, cancellationToken);
            var assets =  meta.Data.Result?.Values
                .SelectMany(x => x.PakAssets)
                .ToList();
            return assets!.Select(file => new
            {
                AssetName = file.Name,
                ModFile = pakFile.LoadoutItemGroup.AsLoadoutItem().Name,
            }).ToList();
        })
        .ToListAsync(cancellationToken: cancellationToken);
        
        var ueAssetLookup = fileTasks
            .SelectMany(task => task.Result)
            .ToLookup(x => x.AssetName, x => x.ModFile);
        
        var diagnostics = ueAssetLookup
            .Where(x => x.Count() > 1)
            .Select(x => Diagnostics.CreateUnrealEngineAssetConflict(
                ConflictingItems: string.Join(", ", x.ToArray()),
                ModifiedUEAsset: x.Key
            ));

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }
}
