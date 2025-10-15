using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Collections;
using NexusMods.Games.CreationEngine.Tests.TestAttributes;
using NexusMods.Games.IntegrationTestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Games.CreationEngine.Tests.SkyrimSE;

[SkyrimSESteamCurrent]
public class CollectionTests(Type gameType, GameLocatorResult locatorResult) : AGameIntegrationTest(gameType, locatorResult)
{
    [Test]
    [Arguments("xk05aw", 229)]
    public async Task InstallCollection(string collectionStub, int revisionNumber)
    {
        var loadout = await CreateLoadout();

        await using var destination = TemporaryFileManager.CreateFile();
        var downloadJob = NexusModsLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From(collectionStub), RevisionNumber.From((ulong)revisionNumber), CancellationToken.None);

        var libraryFile = await LibraryService.AddDownload(downloadJob);
        
        if (!libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile))
            throw new InvalidOperationException("The library file is not a NexusModsCollectionLibraryFile");
        
        var revisionMetadata = await NexusModsLibrary.GetOrAddCollectionRevision(collectionFile, CollectionSlug.From(collectionStub), RevisionNumber.From((ulong)revisionNumber), CancellationToken.None);

        var collectionDownloader = new CollectionDownloader(ServiceProvider);
        await collectionDownloader.DownloadItems(revisionMetadata, itemType: CollectionDownloader.ItemType.Required, db: Connection.Db);

        var items = CollectionDownloader.GetItems(revisionMetadata, CollectionDownloader.ItemType.Required);
        var installJob = await InstallCollectionJob.Create(ServiceProvider, loadout, collectionFile, revisionMetadata, items);
        
        List<(string Mod, GamePath Path, Hash Hash, Size Size, int modCount)> collectionFiles = [];

        foreach (var mod in installJob.AsCollectionGroup().AsLoadoutItemGroup().Children.OfTypeLoadoutItemGroup())
        {
            foreach (var file in mod.Children.OfTypeLoadoutItemWithTargetPath().OfTypeLoadoutFile())
            {
                collectionFiles.Add((mod.AsLoadoutItem().Name, file.AsLoadoutItemWithTargetPath().TargetPath, file.Hash, file.Size, mod.Children.Count));
            }
        }
        
        collectionFiles.Sort();
        await Verify(Table(collectionFiles))
            .UseParameters(collectionStub, revisionNumber)
            .UseDirectory("Verification Files");
    }
}
