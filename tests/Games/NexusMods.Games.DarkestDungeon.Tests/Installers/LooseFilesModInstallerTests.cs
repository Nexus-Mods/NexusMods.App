using FluentAssertions;
using NexusMods.Common;
using NexusMods.Games.DarkestDungeon.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Games.DarkestDungeon.Tests.Installers;

public class LooseFilesModInstallerTests : AModInstallerTest<DarkestDungeon, LooseFilesModInstaller>
{
    public LooseFilesModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_Priority_WithoutProjectFile()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>();
        await using var path = await CreateTestArchive(testFiles);

        var priority = await GetPriorityFromInstaller(path);
        priority.Should().Be(Priority.Lowest);
    }

    [Fact]
    public async Task Test_Priority_WithProjectFile()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { "project.xml", NativeModInstallerTests.CreateModProject(out _) }
        };

        await using var path = await CreateTestArchive(testFiles);

        var priority = await GetPriorityFromInstaller(path);
        priority.Should().Be(Priority.None);
    }

    [Fact]
    public async Task Test_GetMods()
    {
        var testFiles = new Dictionary<RelativePath, byte[]>
        {
            { "foo/bar", Array.Empty<byte>() },
            { "foo/baz", Array.Empty<byte>() }
        };

        await using var path = await CreateTestArchive(testFiles);

        var (_, modFiles) = await GetModWithFilesFromInstaller(path);
        modFiles.Should().HaveCount(2);
        modFiles.Should().Contain(x => x.To.Path.Equals("mods/foo/bar"));
        modFiles.Should().Contain(x => x.To.Path.Equals("mods/foo/baz"));
    }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_InstallMod()
    {
        var loadout = await CreateLoadout();

        // Better Trinkets v2.03 (https://www.nexusmods.com/darkestdungeon/mods/76)
        var (path, hash) = await DownloadMod(GameInstallation.Game.Domain, ModId.From(76), FileId.From(1851));
        await using (path)
        {
            hash.Should().Be(Hash.From(0x068CF757544AA943));

            var mod = await InstallModFromArchiveIntoLoadout(loadout, path);
            mod.Files.Should().NotBeEmpty();
            mod.Files.Should().AllSatisfy(kv => kv.Value.To.Path.StartsWith("mods/Oks_BetterTrinkets_v2.03"));
        }
    }
}
