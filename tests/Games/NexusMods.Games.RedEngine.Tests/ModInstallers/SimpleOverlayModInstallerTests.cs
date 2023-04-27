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
        var (hash1, hash2) = Next2Hash();
        
        var description = await BuildAndInstall(Priority.Normal, 
            (hash1, "bin/x64/foo.exe"),
            (hash2, "archive/pc/mod/foo.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, GameFolderType.Game, "bin/x64/foo.exe"), 
                (hash2, GameFolderType.Game, "archive/pc/mod/foo.archive")
            });
    }
    
    [Fact]
    public async Task FilesUnderSubFoldersAreSupported()
    {
        var (hash1, hash2) = Next2Hash();
        
        var description = await BuildAndInstall(Priority.Normal, 
            (hash1, "mymod/bin/x64/foo.exe"),
            (hash2, "mymod/archive/pc/mod/foo.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, GameFolderType.Game, "bin/x64/foo.exe"), 
                (hash2, GameFolderType.Game, "archive/pc/mod/foo.archive")
            });
    }
    
    [Fact]
    public async Task FilesUnderTwoSubFolderDepthsAreNotSupported()
    {
        var (hash1, hash2) = Next2Hash();
        await BuildAndInstall(Priority.None, 
            (hash1, "prefix/mymod/bin/x64/foo.exe"),
            (hash2, "mymod/archive/pc/mod/foo.archive"));
    }
    
    [Fact]
    public async Task AllCommonPrefixesAreSupported()
    {
        var (hash1, hash2, hash3) = Next3Hash();
        var (hash4, hash5) = Next2Hash();
        
        var files = await BuildAndInstall(Priority.Normal, 
            (hash1, "bin/x64/foo.exe"),
            (hash2, "engine/foo.exe"),
            (hash3, "r6/foo.exe"),
            (hash4, "red4ext/foo.exe"),
            (hash5, "archive/pc/mod/foo.archive"));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, GameFolderType.Game, "bin/x64/foo.exe"),
                (hash2, GameFolderType.Game, "engine/foo.exe"),
                (hash3, GameFolderType.Game, "r6/foo.exe"),
                (hash4, GameFolderType.Game, "red4ext/foo.exe"),
                (hash5, GameFolderType.Game, "archive/pc/mod/foo.archive")
            });
    }

    [Fact]
    public async Task IgnoredExtensionsAreIgnored()
    {
        var (hash1, hash2, hash3) = Next3Hash();
        var (hash4, hash5) = Next2Hash();
        
        var files = await BuildAndInstall(Priority.Normal,
            (hash1, "bin/x64/foo.exe"),
            (hash2, "file.txt"),
            (hash3, "docs/file.md"),
            (hash4, "bin/x64/file.pdf"),
            (hash5, "bin/x64/file.png"));
        
        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, GameFolderType.Game, "bin/x64/foo.exe")
            });
    }
}
