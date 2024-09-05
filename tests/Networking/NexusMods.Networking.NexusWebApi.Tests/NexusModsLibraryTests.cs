using FluentAssertions;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.Tests;

[Trait("RequiresNetworking", "True")]
public class NexusModsLibraryTests
{
    private readonly NexusModsLibrary _library;
    private readonly TemporaryFileManager _temporaryFileManager;
    
    public NexusModsLibraryTests(NexusModsLibrary library, TemporaryFileManager temporaryFileManager)
    {
        _library = library;
        _temporaryFileManager = temporaryFileManager;
    }

    [Fact]
    public async Task CanDownloadCollection()
    {
        await using var destination = _temporaryFileManager.CreateFile();
        var job = await _library.CreateCollectionDownloadJob(destination, CollectionSlug.From("iszwwe"), RevisionNumber.From(469),
            CancellationToken.None
        );
        
        job.FileInfo.Size.Value.Should().Be(20940);
    }

}
