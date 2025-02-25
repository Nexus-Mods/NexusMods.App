using System.Runtime.CompilerServices;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.References;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Extensions.BCL;

namespace NexusMods.Games.UnrealEngine.Emitters;

public class ModOverwritesGameFilesEmitter : ILoadoutDiagnosticEmitter
{
    private static readonly GamePath GameDirectoryPath = new(Constants.GameMainLocationId, "");

    public async IAsyncEnumerable<Diagnostic> Diagnose(
        Loadout.ReadOnly loadout,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var foundGameFilesGroup = LoadoutGameFilesGroup
            .FindByGameMetadata(loadout.Db, loadout.Installation.GameInstallMetadataId)
            .TryGetFirst(x => x.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadout.LoadoutId, out var gameFilesGroup);

        if (!foundGameFilesGroup) yield break;
        
        var gameFilesLookup = gameFilesGroup.AsLoadoutItemGroup().Children
            .Select(gameFile => gameFile.TryGetAsLoadoutItemWithTargetPath(out var targeted)
                    ? (GamePath)targeted.TargetPath
                    : default(GamePath))
            .Where(x => x != default(GamePath))
            .ToLookup(x => x.FileName);

        var items = loadout.Items
            .GetEnabledLoadoutFiles()
            .Where(file =>
            {
                var targetPath = (GamePath)file.AsLoadoutItemWithTargetPath().TargetPath;
                return targetPath.InFolder(GameDirectoryPath) && gameFilesLookup.Contains(targetPath.FileName);
            })
            .Select(file => file.AsLoadoutItemWithTargetPath().AsLoadoutItem())
            .DistinctBy(item => item.Id);

        foreach (var item in items)
        {
            if (item.TryGetAsLoadoutItemGroup(out var res))
            {
                yield return Diagnostics.CreateModOverwritesGameFiles(
                    Group: res.ToReference(loadout),
                    GroupName: item.Name
                );   
            }
        }
    }
}
