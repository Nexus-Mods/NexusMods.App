using FluentAssertions;
using NexusMods.Common;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.ModInstallers;

public class FolderlessModInstallerTests : AModInstallerTest<Cyberpunk2077, FolderlessModInstaller>
{
    public FolderlessModInstallerTests(IServiceProvider serviceProvider) : base(
        serviceProvider)
    {

    }

    [Fact]
    public async Task FilesUnderNoFolderAreSupported()
    {
        var (hash1, hash2) = Next2Hash();

        var description = await BuildAndInstall(
            (hash1, "folder/filea.archive"),
            (hash2, "fileb.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, LocationId.Game, "archive/pc/mod/filea.archive"),
                (hash2, LocationId.Game, "archive/pc/mod/fileb.archive")
            });
    }

    [Fact]
    public async Task IgnoredExtensionsAreIgnored()
    {
        var (hash1, hash2, hash3) = Next3Hash();
        var (hash4, hash5) = Next2Hash();

        var description = await BuildAndInstall(
            (hash1, "folder/filea.archive"),
            (hash2, "file.txt"),
            (hash3, "docs/file.md"),
            (hash4, "bin/x64/file.pdf"),
            (hash5, "bin/x64/file.png"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, LocationId.Game, "archive/pc/mod/filea.archive")
            });
    }
}
