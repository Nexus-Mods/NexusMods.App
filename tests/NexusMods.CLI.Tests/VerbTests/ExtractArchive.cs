using FluentAssertions;
using NexusMods.Paths;

namespace NexusMods.CLI.Tests.VerbTests;

public class ExtractArchive(IServiceProvider provider) : AVerbTest(provider)
{
    [Fact]
    public async Task CanExtractArchive()
    {
        await using var folder = TemporaryFileManager.CreateFolder();

        await Run("extract-archive", "-i", Data7ZipLZMA2.ToString(), "-o", folder.Path.ToString());

        folder.Path.EnumerateFiles().Count().Should().Be(3);

    }
}
