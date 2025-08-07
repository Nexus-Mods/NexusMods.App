using FluentAssertions;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Xunit;

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

    [Theory]
    [Trait("RequiresApiKey", "True")]
    [InlineData("iszwwe", 469)]
    [InlineData("r1flnc", 38)]
    [InlineData("aexcgn", 6)]
    public async Task CanDownloadCollection(string slug, ulong revisionNumber)
    {
        ApiKeyTestHelper.RequireApiKey();
        await using var destination = _temporaryFileManager.CreateFile();
        var downloadJob = _nexusLibrary.CreateCollectionDownloadJob(destination, CollectionSlug.From(slug), RevisionNumber.From(revisionNumber),
            CancellationToken.None
        );
        
        var libraryFile = await _libraryService.AddDownload(downloadJob);
        
        // Make sure the metadata is linked correctly
        libraryFile.TryGetAsNexusModsCollectionLibraryFile(out var collectionFile).Should().BeTrue();
        collectionFile.CollectionRevisionNumber.Value.Should().Be(revisionNumber);
        collectionFile.CollectionSlug.Value.Should().Be(slug);

        // The downloaded file should be the correct size
        libraryFile.Size.Value.Should().BeGreaterThan(0);
        
        // The downloaded file should be a library archive
        libraryFile.TryGetAsLibraryArchive(out var archive).Should().BeTrue();

        // Verify the collection.json file is present and has the correct size
        var collectionJson = archive.Children.First(c => c.Path == "collection.json");
        collectionJson.IsValid().Should().BeTrue();
    }

}
