using FluentAssertions;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.Tests;

[Trait("RequiresNetworking", "True")]
public class NexusModsLibraryTests
{
    private readonly NexusModsLibrary _nexusLibrary;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly ILibraryService _libraryService;

    public NexusModsLibraryTests(NexusModsLibrary nexusLibrary, TemporaryFileManager temporaryFileManager, ILibraryService libraryService)
    {
        _nexusLibrary = nexusLibrary;
        _libraryService = libraryService;
        _temporaryFileManager = temporaryFileManager;
    }

    [Fact]
    public async Task CanDownloadCollection()
    {
        await using var destination = _temporaryFileManager.CreateFile();
        var downloadJob = _nexusLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From("iszwwe"), RevisionNumber.From(469),
            CancellationToken.None
        );
        
        var libraryFile = await _libraryService.AddDownload(downloadJob);
        
        // Make sure the metadata is linked correctly
        libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile).Should().BeTrue();
        collectionFile.CollectionRevision.RevisionNumber.Value.Should().Be(469);
        collectionFile.CollectionRevision.Collection.Slug.Value.Should().Be("iszwwe");

        // The downloaded file should be the correct size
        libraryFile.Size.Value.Should().Be(20940);
        
        // The downloaded file should be a library archive
        libraryFile.TryGetAsLibraryArchive(out var archive).Should().BeTrue();

        // Verify the collection.json file is present and has the correct size
        var collectionJson = archive.Children.First(c => c.Path == "collection.json");
        collectionJson.AsLibraryFile().Size.Value.Should().Be(145406UL);
    }

}
