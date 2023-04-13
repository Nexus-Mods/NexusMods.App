using FluentAssertions;
using NexusMods.Common;
using NexusMods.Games.RedEngine.FileAnalyzers;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.ModInstallers;

public class RedModInstallerTests : AModInstallerTest<Cyberpunk2077, RedModInstaller>
{
    public RedModInstallerTests(IServiceProvider serviceProvider) : base(
        serviceProvider)
    {
        
    }
    
    [Fact]
    public async Task ModsAreDetectedAndInstalled()
    {
        var files = await BuildAndInstall(Priority.High,
            (1, "mymod/info.json", new RedModInfo {Name = "My Mod"}),
            (1, "mymod/blerg.archive", null));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female/cool_choom.preset"), 
                (1, GameFolderType.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male/cool_choom.preset")
            });
    }
}
