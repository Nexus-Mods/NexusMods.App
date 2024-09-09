using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.TestFramework;
using Xunit.Abstractions;

namespace NexusMods.Collections.Tests;

[Trait("RequiresNetworking", "True")]
public class CollectionInstallTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<CollectionInstallTests>(helper)
{

    [Fact]
    public async Task CanInstallBasicCollection()
    {
        await using var destination = TemporaryFileManager.CreateFile();
        var downloadJob = NexusModsLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From("jjctqn"), RevisionNumber.From(1),
            CancellationToken.None
        );
        
        var libraryFile = await LibraryService.AddDownload(downloadJob);
        
        if (!libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile))
            throw new InvalidOperationException("The library file is not a NexusModsCollectionLibraryFile");

        var loadout = await CreateLoadout();
        var installJob = await InstallCollectionJob.Create(ServiceProvider, loadout, collectionFile);

        loadout = loadout.Rebase();

        var mods = loadout.Items.Where(item => !item.Contains(LoadoutItem.ParentId))
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToArray();
        
        var files = loadout.Items
            .OfTypeLoadoutItemWithTargetPath()
            .Select(f => ((GamePath)f.TargetPath).ToString())
            .Order()
            .ToArray();

        await Verify(new {
            mods,
            files
        });
    }
}
