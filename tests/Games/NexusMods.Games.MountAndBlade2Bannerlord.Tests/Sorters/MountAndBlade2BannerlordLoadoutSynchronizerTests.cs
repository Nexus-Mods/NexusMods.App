using FluentAssertions;
using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Games.MountAndBlade2Bannerlord.Tests.Shared;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests.Sorters;

public class MountAndBlade2BannerlordLoadoutSynchronizerTests : AGameTest<MountAndBlade2Bannerlord>
{
    public MountAndBlade2BannerlordLoadoutSynchronizerTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task GeneratedSortRulesAreFetched()
    {
        var loadoutMarker = await CreateLoadout();
        var loadoutSynchronizer = (loadoutMarker.Value.Installation.GetGame().Synchronizer as MountAndBlade2BannerlordLoadoutSynchronizer)!;

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadoutMarker.AddNative(context);
        await loadoutMarker.AddButterLib(context);
        await loadoutMarker.AddHarmony(context);

        var mod = loadoutMarker.Value.Mods.Values.First(m => m.Name == "ButterLib");
        var nameForId = loadoutMarker.Value.Mods.Values.ToDictionary(m => m.Id, m => m.Name);
        var rules = await loadoutSynchronizer.ModSortRules(loadoutMarker.Value, mod);

        var testData = rules.Select(r =>
        {
            if (r is After<Mod, ModId> a) return ("After", nameForId[a.Other]);
            if (r is Before<Mod, ModId> b) return ("Before", nameForId[b.Other]);
            throw new NotImplementedException();
        });

        testData.Should().BeEquivalentTo(new[]
        {
            ("After", "Harmony"),
            ("Before", "Native")
        });
    }
}
