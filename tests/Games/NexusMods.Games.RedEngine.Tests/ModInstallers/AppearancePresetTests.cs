using FluentAssertions;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.RedEngine.Tests.ModInstallers;

public class AppearancePresetTests : AModInstallerTest<Cyberpunk2077, AppearancePreset>
{
    public AppearancePresetTests(IServiceProvider serviceProvider) : base(serviceProvider) { }


    [Fact]
    public async Task PresetFilesAreInstalledCorrectly()
    {
        var hash = NextHash();
        var files = await BuildAndInstall(Priority.Normal,
            (hash, "cool_choom.preset"));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash, LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female/cool_choom.preset"),
                (hash, LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male/cool_choom.preset")
            });
    }

    [Fact]
    public async Task DocumentationFilesAreIgnored()
    {
        var hash = NextHash();
        var files = await BuildAndInstall(Priority.Normal,
            (hash, "cool_choom.preset"),
            (NextHash(), "README.md"),
            (NextHash(), "README.txt"),
            (NextHash(), "README.pdf"));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash, LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female/cool_choom.preset"),
                (hash, LocationId.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male/cool_choom.preset")
            });
    }

}

