using System.Text;
using FluentAssertions;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon.Tests;

public class DarkestDungeonModInstallerTests : AModInstallerTest<DarkestDungeon, DarkestDungeonModInstaller>
{
    public DarkestDungeonModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_WithRealMod()
    {
        // Marvin Seo's Lamia Class Mod: Lamia Main File (Version 1.03)
        var (file, hash) = await DownloadMod(Game.Domain, ModId.From(501), FileId.From(2705));
        hash.Should().Be(Hash.From(0x34C32E580205FC36));

        await using (file)
        {
            var (_, modFiles) = await GetModWithFilesFromInstaller(file.Path);
            modFiles.Should().HaveCount(181);
            modFiles
                .OfType<IToFile>()
                .Should()
                .AllSatisfy(x => x.To.Path.StartsWith("mods"));
        }
    }

    [Fact]
    public async Task Test_WithFakeMod()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        testFiles["archive/modfiles.txt"] = Array.Empty<byte>();
        testFiles["archive/foo"] = Array.Empty<byte>();
        testFiles["archive/bar"] = Array.Empty<byte>();

        var file = await CreateTestArchive(testFiles);
        await using (file)
        {
            var (_, modFiles) = await GetModWithFilesFromInstaller(file.Path);
            modFiles.Should().HaveCount(3);
            modFiles
                .Cast<IToFile>()
                .Should().AllSatisfy(x => x.To.Path.StartsWith("mods"));
            modFiles.Cast<IToFile>().Should().Contain(x => x.To.FileName == "foo");
            modFiles.Cast<IToFile>().Should().Contain(x => x.To.FileName == "bar");
        }
    }
}
