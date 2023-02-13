using FluentAssertions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.CLI.Tests.VerbTests;

public class ExtractArchive : AVerbTest
{
    public ExtractArchive(TemporaryFileManager temporaryFileManager, IServiceProvider provider) : base(temporaryFileManager, provider)
    {
    }

    [Fact]
    public async Task CanExtractArchive()
    {
        var file1 = KnownFolders.EntryFolder.Join(@"Resources\data_7zip_lzma2.7z");
        await using var folder = TemporaryFileManager.CreateFolder();

        await RunNoBanner("extract-archive", "-i", file1.ToString(), "-o", folder.Path.ToString());

        folder.Path.EnumerateFiles().Count().Should().Be(3);

    }
}