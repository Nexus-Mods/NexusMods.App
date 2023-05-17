using FluentAssertions;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests;

public class MountAndBlade2BannerlordTests : AGameTest<MountAndBlade2Bannerlord>
{
    public MountAndBlade2BannerlordTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public void CanFindGames()
    {
        Game.Name.Should().Be("Mount & Blade II: Bannerlord");
        Game.Domain.Should().Be(MountAndBlade2Bannerlord.StaticDomain);
        Game.Installations.Count().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CanGeneratePluginsFile()
    {
        // TODO:
    }
}
