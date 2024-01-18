using System.Text;
using FluentAssertions;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;

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

        var info = "{ \"name\": \"mymod\", \"version\": \"1.8.0\", \"customSounds\": [] }";

        var files = await BuildAndInstall(
            (hash1, "mymod/info.json", Encoding.UTF8.GetBytes(info)),
            (hash2, "mymod/blerg.archive", new byte[]{0xFF}));

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, LocationId.Game, "mods/mymod/info.json"),
                (hash2, LocationId.Game, "mods/mymod/blerg.archive")
            });
    }

    [Fact]
    public async Task MultipleModsInDifferentFoldersAreInstalled()
    {
        var (hash1, hash2, hash3) = Next3Hash();
        var (hash4, hash5, hash6) = Next3Hash();

        var info1 = "{ \"name\": \"mymod1\", \"version\": \"1.8.0\", \"customSounds\": [] }";
        var info2 = "{ \"name\": \"mymod2\", \"version\": \"1.8.0\", \"customSounds\": [] }";
        var info3 = "{ \"name\": \"mymod3\", \"version\": \"1.8.0\", \"customSounds\": [] }";

        var files = await BuildAndInstall(
            (hash1, "mymod1/info.json", Encoding.UTF8.GetBytes(info1)),
            (hash2, "mymod1/blerg.archive", Array.Empty<byte>()),
            (hash3, "mymod2/info.json", Encoding.UTF8.GetBytes(info2)),
            (hash4, "mymod2/blerg.archive", Array.Empty<byte>()),
            (hash5, "optional/mymod3/info.json",Encoding.UTF8.GetBytes(info3)),
            (hash6, "optional/mymod3/blerg.archive", Array.Empty<byte>())
        );

        files.Should()
            .BeEquivalentTo(new[]
            {
                (hash1, LocationId.Game, "mods/mymod1/info.json"),
                (hash2, LocationId.Game, "mods/mymod1/blerg.archive")
            });
    }
}
