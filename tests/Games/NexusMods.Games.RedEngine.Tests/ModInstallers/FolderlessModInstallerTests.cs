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
        var description = await BuildAndInstall(Priority.Low, 
            (1, "folder/filea.archive"),
            (2, "fileb.archive"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "archive/pc/mod/filea.archive"), 
                (2, GameFolderType.Game, "archive/pc/mod/fileb.archive")
            });
    }
    
    [Fact]
    public async Task IgnoredExtensionsAreIgnored()
    {
        var description = await BuildAndInstall(Priority.Low, 
            (1, "folder/filea.archive"),
            (2, "file.txt"),
            (3, "docs/file.md"),
            (4, "bin/x64/file.pdf"),
            (5, "bin/x64/file.png"));

        description.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "archive/pc/mod/filea.archive")
            });
    }
}
