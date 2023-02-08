using FluentAssertions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.Tests.Harness;

namespace NexusMods.DataModel.Tests;

public class ModSortingTests : ADataModelTest<ModSortingTests>
{
    public ModSortingTests(IServiceProvider provider) : base(provider)
    {

    }

    [Fact]
    public async Task ModSortingRulesArePreserved()
    {
        var loadout = await LoadoutManager.ManageGame(Install, Guid.NewGuid().ToString());
        var id1 = await loadout.Install(DATA_7Z_LZMA2, "Mod1", CancellationToken.None);
        var id2 = await loadout.Install(DATA_ZIP_LZMA, "Mod2", CancellationToken.None);

        loadout.Value.Mods.Values.First(m => m.Files.Values.OfType<GameFile>().Any())
            .SortRules.Should().Contain(new First<Mod, ModId>(), "game files are loaded first");
    }
}