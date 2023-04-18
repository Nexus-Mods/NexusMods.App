using FluentAssertions;
using NexusMods.Common;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Tests.ModInstallers;

public class AppearancePresetTests : AModInstallerTest<Cyberpunk2077, AppearancePreset>
{
    public AppearancePresetTests(IServiceProvider serviceProvider) : base(serviceProvider) { }


    [Fact]
    public async Task PresetFilesAreInstalledCorrectly()
    {
        var files = await BuildAndInstall(Priority.Normal,
            (1, "cool_choom.preset", FileType.Cyberpunk2077AppearancePreset));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female/cool_choom.preset"), 
                (1, GameFolderType.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male/cool_choom.preset")
            });
    }
    
    [Fact]
    public async Task DocumentationFilesAreIgnored()
    {
        var files = await BuildAndInstall(Priority.Normal,
            (1, "cool_choom.preset", FileType.Cyberpunk2077AppearancePreset),
            (2, "README.md", FileType.TXT),
            (3, "README.txt", FileType.TXT),
            (4, "README.md", FileType.TXT),
            (5, "README.pdf", FileType.TXT));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (1, GameFolderType.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/female/cool_choom.preset"), 
                (1, GameFolderType.Game, "bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceChangeUnlocker/character-preset/male/cool_choom.preset")
            });
    }

}

