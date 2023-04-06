using FluentAssertions;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Games.DarkestDungeon.Tests;

[Trait("RequiresNetworking", "True")]
public class DarkestDungeonModInstallerTests : AModInstallerTest<DarkestDungeon, DarkestDungeonModInstaller>
{
    public DarkestDungeonModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_ModInstaller()
    {
        // Marvin Seo's Lamia Class Mod: Lamia Main File (Version 1.03)
        var (file, hash) = await DownloadModAsync(Game.Domain, ModId.From(501), FileId.From(2705));
        hash.Should().Be(Hash.From(0x34C32E580205FC36));

        await using (file)
        {
            var filesToExtract = await GetFilesToExtractFromInstaller(file.Path);
            filesToExtract.Should().HaveCount(181);
        }
    }
}
