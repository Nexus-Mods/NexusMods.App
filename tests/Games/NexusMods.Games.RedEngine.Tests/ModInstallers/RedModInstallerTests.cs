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
        var files = await BuildAndInstall(Priority.High,
            (1, "mymod/info.json", new RedModInfo { Name = "My Mod" }),
            (2, "mymod/blerg.archive", null));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "mods/mymod/info.json"),
                (2, GameFolderType.Game, "mods/mymod/blerg.archive")
            });
    }

    [Fact]
    public async Task MultipleModsInDifferentFoldersAreInstalled()
    {
        var files = await BuildAndInstall(Priority.High,
            (1, "mymod1/info.json", new RedModInfo { Name = "My Mod1" }),
            (2, "mymod1/blerg.archive", null),
            (3, "mymod2/info.json", new RedModInfo { Name = "My Mod2" }),
            (4, "mymod2/blerg.archive", null),
            (5, "optional/mymod3/info.json",
                new RedModInfo { Name = "My Mod3" }),
            (6, "optional/mymod3/blerg.archive", null)
        );

        files.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "mods/mymod1/info.json"),
                (2, GameFolderType.Game, "mods/mymod1/blerg.archive"),
                (3, GameFolderType.Game, "mods/mymod2/info.json"),
                (4, GameFolderType.Game, "mods/mymod2/blerg.archive"),
                (5, GameFolderType.Game, "mods/mymod3/info.json"),
                (6, GameFolderType.Game, "mods/mymod3/blerg.archive")
            });
    }
}
