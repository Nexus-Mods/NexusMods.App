using System.Text;
using DynamicData.Kernel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
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
        using var tx = _connection.BeginTransaction();
        var loadoutNew = new Loadout.New(tx)
        {
            Name = "Test Loadout",
            ShortName = "Test",
            InstallationId = GameInstallation.GameMetadataId,
            LoadoutKind = LoadoutKind.Default,
            Revision = 0,
            GameVersion = VanityVersion.From("Unknown"),
        };
        _ = new CollectionGroup.New(tx, out var userCollectionId)
        {
            IsReadOnly = false,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, userCollectionId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, userCollectionId)
                {
                    Name = "My Mods",
                    LoadoutId = loadoutNew.LoadoutId,
                },
            },
        };

        // Commit the loadout to the DB.
        var result = await tx.Commit();
        var loadout = result.Remap(loadoutNew);
        
        // Act
        // Create a new library item (e.g., add a local file)
        var nestedArchivePath = FileSystem.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources")
            .Combine("nested_archive.zip");
        var libraryItem = (await _libraryService.AddLocalFile(nestedArchivePath)).AsLibraryFile().AsLibraryItem();

        // Add it to our new loadout.
        await _libraryService.InstallItem(libraryItem, loadout.LoadoutId);
        var loadouts = _libraryService.LoadoutsWithLibraryItem(libraryItem, _connection.Db);

        // Assert that we have a single item.
        loadouts.Should().ContainSingle()
            .Which.LoadoutId.Should().Be(loadout.LoadoutId);
    }
}
