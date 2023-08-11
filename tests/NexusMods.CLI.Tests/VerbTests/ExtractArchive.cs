using FluentAssertions;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests.VerbTests;

public class ExtractArchive : AVerbTest
{
    public ExtractArchive(TemporaryFileManager temporaryFileManager, IServiceProvider provider) : base(temporaryFileManager, provider)
    {
    }

    [Fact]
    public async Task CanExtractArchive()
    {
        await using var folder = TemporaryFileManager.CreateFolder();

        await RunNoBannerAsync("extract-archive", "-i", Data7ZipLZMA2.ToString(), "-o", folder.Path.ToString());

        folder.Path.EnumerateFiles().Count().Should().Be(3);

    }
}
