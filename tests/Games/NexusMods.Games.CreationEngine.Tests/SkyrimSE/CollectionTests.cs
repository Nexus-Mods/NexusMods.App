using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Collections;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.CreationEngine.Tests.SkyrimSE;

public class CollectionTests(ITestOutputHelper outputHelper) : AIsolatedGameTest<CollectionTests, CreationEngine.SkyrimSE.SkyrimSE>(outputHelper)
{
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddCreationEngine()
            .AddAdapters()
            .AddUniversalGameLocator<CreationEngine.SkyrimSE.SkyrimSE>(new Version("1.6.1"));
    }


    [Theory]
    [InlineData("xk05aw", 229)]
    [Trait("RequiresNetworking", "True")]
    public async Task InstallCollection(string collectionStub, int revisionNumber)
    {
        var loadout = await CreateLoadout();

        ApiKeyTestHelper.RequireApiKey();
        
        var loginManager = ServiceProvider.GetRequiredService<ILoginManager>();
        _ = await loginManager.GetUserInfoAsync();

        loginManager.UserInfo.Should().NotBeNull(because: "this test requires a logged in user");
        loginManager.IsPremium.Should().BeTrue(because: "this test requires premium to automatically download mods");

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
        
        List<(string Mod, GamePath Path, Hash Hash, Size Size, int modCount)> collectionFiles = new();

        foreach (var mod in installJob.AsCollectionGroup().AsLoadoutItemGroup().Children.OfTypeLoadoutItemGroup())
        {
            foreach (var file in mod.Children.OfTypeLoadoutItemWithTargetPath().OfTypeLoadoutFile())
            {
                collectionFiles.Add((mod.AsLoadoutItem().Name, file.AsLoadoutItemWithTargetPath().TargetPath, file.Hash, file.Size, mod.Children.Count));
            }
        }
        
        collectionFiles.Sort();
        await VerifyTable(collectionFiles);
    }
    
}
