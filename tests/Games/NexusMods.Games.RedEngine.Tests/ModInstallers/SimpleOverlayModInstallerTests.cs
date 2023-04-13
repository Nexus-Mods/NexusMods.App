using FluentAssertions;
using NexusMods.Common;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.ModInstallers;

public class SimpleOverlayModInstallerTests : AModInstallerTest<Cyberpunk2077, SimpleOverlayModInstaller>
{
    public SimpleOverlayModInstallerTests(IServiceProvider provider) : base(provider)
    {
    }

    [Fact]
    public async Task FilesUnderNoFolderAreSupported()
    {
        var description = await BuildAndInstall(Priority.Normal, 
            (1, "bin/x64/foo.exe"),
            (2, "archive/pc/mod/foo.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "bin/x64/foo.exe"), 
                (2, GameFolderType.Game, "archive/pc/mod/foo.archive")
            });
    }
    
    [Fact]
    public async Task FilesUnderSubFoldersAreSupported()
    {
        var description = await BuildAndInstall(Priority.Normal, 
            (1, "mymod/bin/x64/foo.exe"),
            (2, "mymod/archive/pc/mod/foo.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "bin/x64/foo.exe"), 
                (2, GameFolderType.Game, "archive/pc/mod/foo.archive")
            });
    }
    
    [Fact]
    public async Task FilesUnderTwoSubFolderDepthsAreNotSupported()
    {
        await BuildAndInstall(Priority.None, 
            (1, "prefix/mymod/bin/x64/foo.exe"),
            (2, "mymod/archive/pc/mod/foo.archive"));
    }
    
    [Fact]
    public async Task AllCommonPrefixesAreSupported()
    {
        var files = await BuildAndInstall(Priority.Normal, 
            (1, "bin/x64/foo.exe"),
            (2, "engine/foo.exe"),
            (3, "r6/foo.exe"),
            (4, "red4ext/foo.exe"),
            (5, "archive/pc/mod/foo.archive"));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "bin/x64/foo.exe"),
                (2, GameFolderType.Game, "engine/foo.exe"),
                (3, GameFolderType.Game, "r6/foo.exe"),
                (4, GameFolderType.Game, "red4ext/foo.exe"),
                (5, GameFolderType.Game, "archive/pc/mod/foo.archive")
            });
    }

    [Fact]
    public async Task IgnoredExtensionsAreIgnored()
    {
        var files = await BuildAndInstall(Priority.Normal,
            (1, "bin/x64/foo.exe"),
            (2, "file.txt"),
            (3, "docs/file.md"),
            (4, "bin/x64/file.pdf"),
            (5, "bin/x64/file.png"));
        
        files.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "bin/x64/foo.exe")
            });
    }
}
