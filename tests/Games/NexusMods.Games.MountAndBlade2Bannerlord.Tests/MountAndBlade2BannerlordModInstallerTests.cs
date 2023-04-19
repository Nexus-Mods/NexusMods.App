using FluentAssertions;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests;

public class MountAndBlade2BannerlordModInstallerTests : AModInstallerTest<MountAndBlade2Bannerlord, MountAndBlade2BannerlordModInstaller>
{
    public MountAndBlade2BannerlordModInstallerTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    [Trait("RequiresNetworking", "True")]
    public async Task Test_WithRealMod()
    {
        // TODO:
        /*
        // Harmony: Harmony Main File (Version 2.3.0)
        var (file, hash) = await DownloadMod(Game.Domain, ModId.From(2006), FileId.From(34666));
        hash.Should().Be(Hash.From(0x3FD3503D19DAE052));

        await using (file)
        {
            var filesToExtract = await GetFilesToExtractFromInstaller(file.Path);
            filesToExtract.Should().HaveCount(181);
            filesToExtract.Should().AllSatisfy(x => x.To.Path.StartsWith("mods"));
        }
        */
    }
}
