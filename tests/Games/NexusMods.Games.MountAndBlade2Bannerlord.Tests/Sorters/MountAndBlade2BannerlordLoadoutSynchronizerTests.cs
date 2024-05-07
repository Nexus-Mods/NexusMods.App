using FluentAssertions;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts.Ids;
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
        var loadout = await CreateLoadout();
        var loadoutSynchronizer = (loadout.Installation.GetGame().Synchronizer as MountAndBlade2BannerlordLoadoutSynchronizer)!;

        var context = AGameTestContext.Create(CreateTestArchive, InstallModStoredFileIntoLoadout);

        await loadout.AddNative(context);
        await loadout.AddButterLib(context);
        await loadout.AddHarmony(context);

        Refresh(ref loadout);
        var mod = loadout.Mods.First(m => m.Name == "ButterLib");
        var nameForId = loadout.Mods.ToDictionary(m => ModId.From(m.Id), m => m.Name);
        var rules = await loadoutSynchronizer.ModSortRules(loadout, mod);

        var testData = rules.Select(r =>
        {
            if (r is After<Mod.Model, ModId> a) return ("After", nameForId[a.Other]);
            if (r is Before<Mod.Model, ModId> b) return ("Before", nameForId[b.Other]);
            throw new NotImplementedException();
        });

        testData.Should().BeEquivalentTo(new[]
        {
            ("After", "Harmony"),
            ("Before", "Native")
        });
    }
}
