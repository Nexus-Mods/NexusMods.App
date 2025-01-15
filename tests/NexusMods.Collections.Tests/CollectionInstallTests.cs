using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.TestFramework;
using Xunit.Abstractions;

namespace NexusMods.Collections.Tests;

[Trait("RequiresNetworking", "True")]
public class CollectionInstallTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<CollectionInstallTests>(helper)
{
    [Theory]
    // Includes a basic collection
    [InlineData("jjctqn", 1)]
    // FOMOD and binary patching
    [InlineData("jjctqn", 3)]
    // Includes bundled mod
    [InlineData("jjctqn", 4)]
    // Includes direct download mod
    [InlineData("jjctqn", 5)]
    // Includes a browse mod that can be downloaded directly
    [InlineData("jjctqn", 6)]
    public async Task CanInstallCollections(string slug, int revisionNumber)
    {
        // NOTE(erri120): dirty hack to get the login manager to understand we're premium with the API key
        var loginManager = ServiceProvider.GetRequiredService<ILoginManager>();
        _ = await loginManager.GetUserInfoAsync();

        loginManager.UserInfo.Should().NotBeNull(because: "this test requires a logged in user");
        loginManager.IsPremium.Should().BeTrue(because: "this test requires premium to automatically download mods");

        await using var destination = TemporaryFileManager.CreateFile();
        var downloadJob = NexusModsLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From(slug), RevisionNumber.From((ulong)revisionNumber), CancellationToken.None);

        var libraryFile = await LibraryService.AddDownload(downloadJob);
        
        if (!libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile))
            throw new InvalidOperationException("The library file is not a NexusModsCollectionLibraryFile");

        var loadout = await CreateLoadout();

        var revisionMetadata = await NexusModsLibrary.GetOrAddCollectionRevision(collectionFile, CollectionSlug.From(slug), RevisionNumber.From((ulong)revisionNumber), CancellationToken.None);

        var collectionDownloader = new CollectionDownloader(ServiceProvider);
        await collectionDownloader.DownloadItems(revisionMetadata, itemType: CollectionDownloader.ItemType.Required, db: Connection.Db);

        var items = CollectionDownloader.GetItems(revisionMetadata, CollectionDownloader.ItemType.Required);
        var installJob = await InstallCollectionJob.Create(ServiceProvider, loadout, collectionFile, revisionMetadata, items);

        loadout = loadout.Rebase();

        var mods = loadout.Items.Where(item => !item.Contains(LoadoutItem.ParentId))
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToArray();

        var files = loadout.Items
            .OfTypeLoadoutItemWithTargetPath()
            .OfTypeLoadoutFile()
            .Select(f =>
                {
                    var group = f.AsLoadoutItemWithTargetPath().AsLoadoutItem().Parent.AsLoadoutItem().Name;
                    return KeyValuePair.Create(
                        (group, ((GamePath)f.AsLoadoutItemWithTargetPath().TargetPath).ToString()),
                        f.Hash.ToString()
                    );
                }
            )
            .ToDictionary();

        await Verify(new
            {
                mods,
                files
            }
        ).UseParameters(slug, revisionNumber);
    }
}
