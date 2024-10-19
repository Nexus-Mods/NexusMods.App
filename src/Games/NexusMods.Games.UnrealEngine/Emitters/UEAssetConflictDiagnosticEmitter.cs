using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using Avalonia.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Paths;
using Diagnostic = NexusMods.Abstractions.Diagnostics.Diagnostic;

namespace NexusMods.Games.UnrealEngine.Emitters;

public class UEAssetConflictDiagnosticEmitter : ILoadoutDiagnosticEmitter
{
    private readonly IFileStore _fileStore;

    public UEAssetConflictDiagnosticEmitter(
        IServiceProvider serviceProvider,
        IFileStore fileStore)
    {
        _fileStore = fileStore;
    }

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var ueassetRegex = Constants.UEObjectsRegex();

        var files = loadout.Items
            .GetEnabledLoadoutFiles()
            .Where(file =>
            {
                var loadoutItem = file.AsLoadoutItemWithTargetPath().AsLoadoutItem();
                if (loadoutItem.ParentId == default(LoadoutItemGroupId)) return false;
                return !loadoutItem.Parent.TryGetAsLoadoutGameFilesGroup(out _);
            });

        var fileTasks = await files.ToAsyncEnumerable()
        .Select(async file =>
        {
            await using var stream = await _fileStore.GetFileStream(file.Hash, token: cancellationToken);
            using var reader = new StreamReader(stream);

            // TODO: minimize memory consumption; chunks?
            // TODO: cache this operation; pipelines?
            var content = await reader.ReadToEndAsync();

            var matches = ueassetRegex.Matches(content).Cast<Match>();

            return matches.Select(match => new
            {
                UAsset = match.Value,
                ModFile = file
            }).ToList();
        })
        .ToListAsync(cancellationToken: cancellationToken);

        var ueassetLookup = fileTasks
            .SelectMany(task => task.Result)
            .Where(x => x != null)
            .ToLookup(x => x.UAsset, x => x.ModFile);

        var diagnostics = ueassetLookup
            .Where(x => x.Count() > 1)
            .Select(x => Diagnostics.CreateUEAssetConflict(
                ConflictingItems: string.Join(", ", x.ToArray().Select(x => x.AsLoadoutItemWithTargetPath().AsLoadoutItem().Name)),
                ModifiedUEAsset: x.Key
            ));

        foreach (var diagnostic in diagnostics)
        {
            yield return diagnostic;
        }
    }
}
