using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Collections;
using NexusMods.Games.IntegrationTestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.Games.IntegrationTests.CreationEngine.Tests;

[Fallout4SteamCurrent]
public class Fallout4CollectionTests(Type gameType, GameLocatorResult locatorResult) : AGameIntegrationTest(gameType, locatorResult)
{
    [InlineData("Step 1 - Foundation & Fixes", "cwck5b", 3)]
    public async Task InstallCollection(string name, string collectionStub, int revisionNumber)
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
        
        List<(string Mod, GamePath Path, Hash Hash, Size Size)> collectionFiles = new();

        foreach (var mod in installJob.AsCollectionGroup().AsLoadoutItemGroup().Children.OfTypeLoadoutItemGroup())
        {
            foreach (var file in mod.Children.OfTypeLoadoutItemWithTargetPath().OfTypeLoadoutFile())
            {
                collectionFiles.Add((mod.AsLoadoutItem().Name, file.AsLoadoutItemWithTargetPath().TargetPath, file.Hash, file.Size));
            }
        }

        collectionFiles.Sort();
        await VerifyTable(collectionFiles, name);
    }
}
