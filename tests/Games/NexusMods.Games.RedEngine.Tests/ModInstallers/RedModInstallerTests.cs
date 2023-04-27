using FluentAssertions;
using NexusMods.Common;
using NexusMods.Games.RedEngine.FileAnalyzers;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.ModInstallers;

public class
    RedModInstallerTests : AModInstallerTest<Cyberpunk2077, RedModInstaller>
{
    public RedModInstallerTests(IServiceProvider serviceProvider) : base(
        serviceProvider) { }

    [Fact]
    public async Task ModsAreDetectedAndInstalled()
    {
        var (hash1, hash2) = Next2Hash();
        
        var files = await BuildAndInstall(Priority.High,
            (hash1, "mymod/info.json", new RedModInfo { Name = "My Mod" }),
            (hash2, "mymod/blerg.archive", null));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, GameFolderType.Game, "mods/mymod/info.json"),
                (hash2, GameFolderType.Game, "mods/mymod/blerg.archive")
            });
    }

    [Fact]
    public async Task MultipleModsInDifferentFoldersAreInstalled()
    {
        var (hash1, hash2, hash3) = Next3Hash();
        var (hash4, hash5, hash6) = Next3Hash();
        
        var files = await BuildAndInstall(Priority.High,
            (hash1, "mymod1/info.json", new RedModInfo { Name = "My Mod1" }),
            (hash2, "mymod1/blerg.archive", null),
            (hash3, "mymod2/info.json", new RedModInfo { Name = "My Mod2" }),
            (hash4, "mymod2/blerg.archive", null),
            (hash5, "optional/mymod3/info.json",
                new RedModInfo { Name = "My Mod3" }),
            (hash6, "optional/mymod3/blerg.archive", null)
        );

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, GameFolderType.Game, "mods/mymod1/info.json"),
                (hash2, GameFolderType.Game, "mods/mymod1/blerg.archive"),
                (hash3, GameFolderType.Game, "mods/mymod2/info.json"),
                (hash4, GameFolderType.Game, "mods/mymod2/blerg.archive"),
                (hash5, GameFolderType.Game, "mods/mymod3/info.json"),
                (hash6, GameFolderType.Game, "mods/mymod3/blerg.archive")
            });
    }
}
