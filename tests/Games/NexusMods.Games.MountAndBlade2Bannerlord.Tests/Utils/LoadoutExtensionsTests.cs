using FluentAssertions;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Utils;

public class LoadoutExtensionsTests : AGameTest<MountAndBlade2Bannerlord>
{
    public LoadoutExtensionsTests(IServiceProvider serviceProvider) : base(serviceProvider) { }

    [Fact]
    public async Task Test_GetViewModels()
    {
        var loadoutMarker = await CreateLoadout();

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadoutMarker.AddButterLib(context);
        await loadoutMarker.AddHarmony(context);

        var unsorted = loadoutMarker.Value.GetViewModels().Select(x => x.Mod.Name).ToList();
        var sorted = (await loadoutMarker.Value.GetSortedViewModelsAsync()).Select(x => x.Mod.Name).ToList();

        unsorted.Should().BeEquivalentTo(new[]
        {
            "ButterLib",
            "Harmony",
        });
        sorted.Should().BeEquivalentTo(new[]
        {
            "Harmony",
            "ButterLib",
        });
    }
}
