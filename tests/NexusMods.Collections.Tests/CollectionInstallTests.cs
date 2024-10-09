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

    [Theory]
    [InlineData("jjctqn", 1)]
    [InlineData("jjctqn", 3)]
    public async Task CanInstallCollections(string slug, int revisionNumber)
    {
        await using var destination = TemporaryFileManager.CreateFile();
        var downloadJob = NexusModsLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From(slug), RevisionNumber.From((ulong)revisionNumber),
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
            .OfTypeLoadoutFile()
            .Select(f => KeyValuePair.Create(((GamePath)f.AsLoadoutItemWithTargetPath().TargetPath).ToString(), f.Hash.ToString()))
            .ToDictionary();

        await Verify(new
            {
                mods,
                files
            }
        ).UseParameters(slug, revisionNumber);
    }
}
