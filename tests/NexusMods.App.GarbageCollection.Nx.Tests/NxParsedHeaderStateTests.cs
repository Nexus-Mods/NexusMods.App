using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using Xunit;
namespace NexusMods.App.GarbageCollection.Nx.Tests;

public class NxParsedHeaderStateTests
{
    [Theory, AutoData]
    public void CanUseNxParsedHeaders(IFixture fixture)
    {
        // Act: Setup Dummy Nx Archive
        var files = GetRandomDummyFiles(fixture, out var settings);
        NxPacker.Pack(files, settings);
        settings.Output.Position = 0;
        var streamProvider = new FromStreamProvider(settings.Output);
        var initialHeader = HeaderParser.ParseHeader(streamProvider);

        // Act: Setup
        var parsedHeaderState = new NxParsedHeaderState(initialHeader);
        var hashes = parsedHeaderState.GetFileHashes();
        for (var x = 0; x < hashes.Length; x++)
        {
            var original = initialHeader.Entries[x];
            var casted = hashes[x];
            original.Hash.Should().Be(casted.Hash.Value);
        }
    }

    private static PackerFile[] GetRandomDummyFiles(IFixture fixture, out PackerSettings settings, int numFiles = 64, int minFileSize = 1024, int maxFileSize = 4096)
    {
        var output = new MemoryStream();
        settings = new PackerSettings
        {
            Output = output,
            BlockSize = 32767,
            ChunkSize = 1048576,
            MaxNumThreads = Environment.ProcessorCount,
        };

        var random = new Random();
        var index = 0;
        fixture.Customize<PackerFile>(c =>
        {
            return c.FromFactory(() =>
            {
                var fileSize = random.Next(minFileSize, maxFileSize);
                return new PackerFile()
                {
                    FileSize = fileSize,
                    RelativePath = $"File_{index++}",
                    FileDataProvider = new FromArrayProvider
                    {
                        Data = MakeDummyFile(fileSize),
                    },
                };
            }).OmitAutoProperties();
        });

        return fixture.CreateMany<PackerFile>(numFiles).ToArray();
    }

    private static byte[] MakeDummyFile(int length)
    {
        var result = GC.AllocateUninitializedArray<byte>(length);
        for (var x = 0; x < length; x++)
            result[x] = (byte)(x % 255);

        return result;
    }
}
