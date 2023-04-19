using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

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
