using FluentAssertions;
using NexusMods.DataModel.Abstractions.Games;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.RedEngine.Tests.ModInstallers;

public class SimpleOverlayModInstallerTests : AModInstallerTest<Cyberpunk2077, SimpleOverlayModInstaller>
{
    public SimpleOverlayModInstallerTests(IServiceProvider provider) : base(provider)
    {
    }

    [Fact]
    public async Task FilesUnderNoFolderAreSupported()
    {
        var (hash1, hash2) = Next2Hash();

        var description = await BuildAndInstall(
            (hash1, "bin/x64/foo.exe"),
            (hash2, "archive/pc/mod/foo.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, LocationId.Game, "bin/x64/foo.exe"),
                (hash2, LocationId.Game, "archive/pc/mod/foo.archive")
            });
    }

    [Fact]
    public async Task FilesUnderSubFoldersAreSupported()
    {
        var (hash1, hash2) = Next2Hash();

        var description = await BuildAndInstall(
            (hash1, "mymod/bin/x64/foo.exe"),
            (hash2, "mymod/archive/pc/mod/foo.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, LocationId.Game, "bin/x64/foo.exe"),
                (hash2, LocationId.Game, "archive/pc/mod/foo.archive")
            });
    }

    [Fact]
    public async Task FilesUnderTwoSubFolderDepthsAreNotSupported()
    {
        var (hash1, hash2) = Next2Hash();
        await BuildAndInstall(
            (hash1, "prefix/mymod/bin/x64/foo.exe"),
            (hash2, "mymod/archive/pc/mod/foo.archive"));
    }

    [Fact]
    public async Task AllCommonPrefixesAreSupported()
    {
        var (hash1, hash2, hash3) = Next3Hash();
        var (hash4, hash5) = Next2Hash();

        var files = await BuildAndInstall(
            (hash1, "bin/x64/foo.exe"),
            (hash2, "engine/foo.exe"),
            (hash3, "r6/foo.exe"),
            (hash4, "red4ext/foo.exe"),
            (hash5, "archive/pc/mod/foo.archive"));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, LocationId.Game, "bin/x64/foo.exe"),
                (hash2, LocationId.Game, "engine/foo.exe"),
                (hash3, LocationId.Game, "r6/foo.exe"),
                (hash4, LocationId.Game, "red4ext/foo.exe"),
                (hash5, LocationId.Game, "archive/pc/mod/foo.archive")
            });
    }
}
