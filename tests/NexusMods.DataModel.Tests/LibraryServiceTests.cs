using System.Text;
using DynamicData.Kernel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.Sdk.FileStore;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Tests;

public class LibraryServiceTests : ACyberpunkIsolatedGameTest<LibraryServiceTests>
{
    private readonly ILibraryService _libraryService;
    private readonly IFileStore _fileStore;
    private readonly IConnection _connection;

    public LibraryServiceTests(ITestOutputHelper helper) : base(helper)
    {
        _libraryService = ServiceProvider.GetRequiredService<ILibraryService>();
        _fileStore = ServiceProvider.GetRequiredService<IFileStore>();
        _connection = ServiceProvider.GetRequiredService<IConnection>();
    }

    [Fact]
    public async Task Test_Issue3156()
    {
        const string fileName = "zip-with-encoding.zip";
        var archivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine(fileName);
        archivePath.FileExists.Should().BeTrue();

        var act = async () => await _libraryService.AddLocalFile(archivePath);
        await act.Should().ThrowAsync<PathException>(because: "archive encoding isn't UTF-8 and extracting results in invalid unicode characters");
    }

    [Fact]
    public async Task Test_Issue3003()
    {
        const string fileName = "zip-with-spaces.zip";
        var archivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine(fileName);
        archivePath.FileExists.Should().BeTrue();

        var localFile = await _libraryService.AddLocalFile(archivePath);
        localFile.AsLibraryFile().TryGetAsLibraryArchive(out var libraryArchive).Should().BeTrue();
        libraryArchive.IsValid().Should().BeTrue();

        libraryArchive.Children.Should().Contain(x => x.Path.ToString() == "bar/foo/bar.txt");
    }

    [Fact]
    public async Task KnownNestedArchiveIsExtracted()
    {
        // Archives with known extensions should be extracted and have their contents analyzed
        
        var nestedArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine("nested_archive.zip");
        var libraryItem = await _libraryService.AddLocalFile(nestedArchivePath);
        libraryItem.AsLibraryFile().TryGetAsLibraryArchive(out var archive).Should().BeTrue();
        
        // find the nested archive
        var nestedArchive = archive.Children.FirstOrOptional(x => x.Path.FileName == "someNestedArchive.7z");
        nestedArchive.HasValue.Should().BeTrue();
        
        // Check if the nested archive has associated Archive data
        var nestedLibraryFile = nestedArchive.Value.AsLibraryFile();
        nestedLibraryFile.TryGetAsLibraryArchive(out var nestedLibraryArchive).Should().BeTrue();
        
        // print the contents of the parent and nested archive
        StringBuilder verifyText = new();
        verifyText.AppendLine("Parent Archive:");
        verifyText.Append(PrintArchiveContents(archive));
        verifyText.AppendLine("Nested Archive:");
        verifyText.Append(PrintArchiveContents(nestedLibraryArchive));
        
        await Verify(verifyText.ToString());
    }
    
    
    [Fact]
    public async Task UnknownNestedArchiveIsNotExtracted()
    {
        // Archives with unknown extensions (likely to be valid mod files) should not be extracted and analyzed
        
        var nestedArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine("nested_game_archive.zip");
        var libraryItem = await _libraryService.AddLocalFile(nestedArchivePath);
        libraryItem.AsLibraryFile().TryGetAsLibraryArchive(out var archive).Should().BeTrue();
        
        // find the nested archive
        var nestedArchive = archive.Children.FirstOrOptional(x => x.Path.FileName == "SomeModFile.archive");
        nestedArchive.HasValue.Should().BeTrue();
        
        // Check if the nested archive has associated Archive data
        var nestedLibraryFile = nestedArchive.Value.AsLibraryFile();
        nestedLibraryFile.TryGetAsLibraryArchive(out _).Should().BeFalse();
        
        // File should be available in the file store
        var isStored = await _fileStore.HaveFile(nestedLibraryFile.Hash);
        isStored.Should().BeTrue();
        
        // print the contents of the parent archive
        await Verify(PrintArchiveContents(archive));
    }
    
    
    private async ValueTask<string> PrintArchiveContents(LibraryArchive.ReadOnly archive)
    {
        var result = new StringBuilder();
        foreach (var entry in archive.Children.OrderBy(x => x.Path))
        {
            result.AppendLine($"{entry.Path} - {entry.AsLibraryFile().Size} -  {entry.AsLibraryFile().Hash} - Stored : {await _fileStore.HaveFile(entry.AsLibraryFile().Hash)}");
        }

        return result.ToString();
    }

    [Fact]
    public async Task LoadoutsWithLibraryItem_ShouldReturnCorrectLoadouts()
    {
        // Arrange the DB
        // Create a new loadout
        var loadout = await CreateTestLoadout("Test Loadout");
        var collection = await CreateCollectionInLoadout(loadout, "Test Collection");
        
        // Act
        // Create a new library item (e.g., add a local file)
        var nestedArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources")
            .Combine("nested_archive.zip");
        var libraryItem = (await _libraryService.AddLocalFile(nestedArchivePath)).AsLibraryFile().AsLibraryItem();

        // Add it to our new loadout.
        await LoadoutManager.InstallItem(libraryItem, loadout.LoadoutId, parent: collection.AsLoadoutItemGroup().LoadoutItemGroupId);
        var loadouts = _libraryService.LoadoutsWithLibraryItem(libraryItem);

        // Assert that we have a single item.
        loadouts.Should().ContainSingle()
            .Which.loadout.LoadoutId.Should().Be(loadout.LoadoutId);
    }

    [Fact]
    public async Task ReplaceLinkedItemsInAllLoadouts_ShouldReplaceItemsAcrossLoadouts()
    {
        // Arrange
        // Create two loadouts
        var loadout1 = await CreateTestLoadout("Loadout1");
        var loadout2 = await CreateTestLoadout("Loadout2");
        var collection1 = await CreateCollectionInLoadout(loadout1, "Test Collection 1");
        var collection2 = await CreateCollectionInLoadout(loadout2, "Test Collection 2");
        
        // Create test files to use as our library items
        var firstArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources")
            .Combine("nested_archive.zip");
        var secondArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources")
            .Combine("nested_game_archive.zip");

        // Add files to library
        var oldItem1 = (await _libraryService.AddLocalFile(firstArchivePath)).AsLibraryFile().AsLibraryItem();
        var oldItem2 = (await _libraryService.AddLocalFile(secondArchivePath)).AsLibraryFile().AsLibraryItem();

        // Install old items in both loadouts
        await LoadoutManager.InstallItem(oldItem1, loadout1.LoadoutId, parent: collection1.AsLoadoutItemGroup().LoadoutItemGroupId);
        await LoadoutManager.InstallItem(oldItem2, loadout1.LoadoutId, parent: collection1.AsLoadoutItemGroup().LoadoutItemGroupId);
        await LoadoutManager.InstallItem(oldItem1, loadout2.LoadoutId, parent: collection2.AsLoadoutItemGroup().LoadoutItemGroupId);

        // Act
        // Replace both items across all loadouts
        var newItem1 = (await _libraryService.AddLocalFile(firstArchivePath)).AsLibraryFile().AsLibraryItem(); // Same content but different LibraryItem instance
        var newItem2 = (await _libraryService.AddLocalFile(secondArchivePath)).AsLibraryFile().AsLibraryItem();
        var replacements = new List<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)>
        {
            (oldItem1, newItem1),
            (oldItem2, newItem2),
        };

        var options = new ReplaceLibraryItemsOptions
        {
            IgnoreReadOnlyCollections = false,
        };
        var result = await _libraryService.ReplaceLinkedItemsInAllLoadouts(replacements, options);

        // Assert
        result.Should().Be(LibraryItemReplacementResult.Success);

        // Verify that the old items are no longer in any loadout
        var loadoutsWithOldItem1 = _libraryService.LoadoutsWithLibraryItem(oldItem1);
        var loadoutsWithOldItem2 = _libraryService.LoadoutsWithLibraryItem(oldItem2);

        loadoutsWithOldItem1.Should().BeEmpty();
        loadoutsWithOldItem2.Should().BeEmpty();

        // Verify that new items are now in the appropriate loadouts
        var loadoutsWithNewItem1 = _libraryService.LoadoutsWithLibraryItem(newItem1);
        var loadoutsWithNewItem2 = _libraryService.LoadoutsWithLibraryItem(newItem2);

        loadoutsWithNewItem1.Should().HaveCount(2)
            .And.Contain(l => l.loadout.LoadoutId == loadout1.LoadoutId)
            .And.Contain(l => l.loadout.LoadoutId == loadout2.LoadoutId);

        loadoutsWithNewItem2.Should().ContainSingle()
            .Which.loadout.LoadoutId.Should().Be(loadout1.LoadoutId);
    }

    [Fact]
    public async Task ReplaceLinkedItemsInAllLoadouts_ShouldRespectReadOnlyCollectionFilter()
    {
        // Arrange
        // Create two loadouts
        var loadout1 = await CreateTestLoadout("Loadout1");
        var loadout2 = await CreateTestLoadout("Loadout2");
        
        // Create a mutable collection in loadout1 and a read-only collection in loadout2
        var mutableCollection = await CreateCollectionInLoadout(loadout1, "Mutable Collection");
        var readOnlyCollection = await CreateCollectionInLoadout(loadout2, "ReadOnly Collection", isReadOnly: true);
        
        // Create test files to use as our library items
        var firstArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources")
            .Combine("nested_archive.zip");
        var secondArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources")
            .Combine("nested_game_archive.zip");

        // Add files to library
        var oldItem1 = (await _libraryService.AddLocalFile(firstArchivePath)).AsLibraryFile().AsLibraryItem();
        var oldItem2 = (await _libraryService.AddLocalFile(secondArchivePath)).AsLibraryFile().AsLibraryItem();

        // Install old items in both loadouts - one in mutable collection, one in read-only collection
        await LoadoutManager.InstallItem(oldItem1, loadout1.LoadoutId, parent: mutableCollection.AsLoadoutItemGroup().LoadoutItemGroupId);
        await LoadoutManager.InstallItem(oldItem2, loadout2.LoadoutId, parent: readOnlyCollection.AsLoadoutItemGroup().LoadoutItemGroupId);

        // Act
        // Create new items to replace the old ones
        var newItem1 = (await _libraryService.AddLocalFile(firstArchivePath)).AsLibraryFile().AsLibraryItem(); // Same content but different LibraryItem instance
        var newItem2 = (await _libraryService.AddLocalFile(secondArchivePath)).AsLibraryFile().AsLibraryItem();
        var replacements = new List<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)>
        {
            (oldItem1, newItem1),
            (oldItem2, newItem2),
        };

        // Set IgnoreReadOnlyCollections to true to ignore read-only collections
        var options = new ReplaceLibraryItemsOptions
        {
            IgnoreReadOnlyCollections = true,
        };
        var result = await _libraryService.ReplaceLinkedItemsInAllLoadouts(replacements, options);

        // Assert
        result.Should().Be(LibraryItemReplacementResult.Success);

        // Verify that the old item in mutable collection is no longer in any loadout
        var loadoutsWithOldItem1 = _libraryService.LoadoutsWithLibraryItem(oldItem1);
        loadoutsWithOldItem1.Should().BeEmpty("because item in mutable collection should be replaced");

        // Verify that the old item in read-only collection is still there (was not replaced)
        var loadoutsWithOldItem2 = _libraryService.LoadoutsWithLibraryItem(oldItem2);
        loadoutsWithOldItem2.Should().ContainSingle("because item in read-only collection should not be replaced")
            .Which.loadout.LoadoutId.Should().Be(loadout2.LoadoutId);

        // Verify that new item1 is now in mutable collection loadout
        var loadoutsWithNewItem1 = _libraryService.LoadoutsWithLibraryItem(newItem1);
        loadoutsWithNewItem1.Should().ContainSingle("because it should only replace the item in the mutable collection")
            .Which.loadout.LoadoutId.Should().Be(loadout1.LoadoutId);

        // Verify that new item2 is not in any loadout (because it wasn't installed due to read-only filter)
        var loadoutsWithNewItem2 = _libraryService.LoadoutsWithLibraryItem(newItem2);
        loadoutsWithNewItem2.Should().BeEmpty("because the item in read-only collection should not be replaced");
    }

    private async Task<Loadout.ReadOnly> CreateTestLoadout(string name)
    {
        using var tx = _connection.BeginTransaction();
        var loadoutNew = new Loadout.New(tx)
        {
            Name = name,
            ShortName = name,
            InstallationId = GameInstallation.GameMetadataId,
            LoadoutKind = LoadoutKind.Default,
            Revision = 0,
            GameVersion = VanityVersion.From("Unknown"),
        };

        var result = await tx.Commit();
        return result.Remap(loadoutNew);
    }

    private async Task<CollectionGroup.ReadOnly> CreateCollectionInLoadout(
        LoadoutId loadoutId,
        string name = "My Mods",
        bool isReadOnly = false)
    {
        using var tx = _connection.BeginTransaction();

        var group = new CollectionGroup.New(tx, out var collectionId)
        {
            IsReadOnly = isReadOnly,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, collectionId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, collectionId)
                {
                    Name = name,
                    LoadoutId = loadoutId,
                },
            },
        };

        var result = await tx.Commit();
        return result.Remap(group);
    }
}
