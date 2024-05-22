using FluentAssertions;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Pages.LoadoutGrid;

namespace NexusMods.UI.Tests.RightContent;

public class LoadoutGridViewModelTests(IServiceProvider provider) : AVmTest<ILoadoutGridViewModel>(provider)
{
    [Fact]
    public async Task AddingModUpdatesTheModSource()
    {
        await CreateLoadout();
        Vm.LoadoutId = Loadout.LoadoutId;

        var ids = await InstallMod(DataZipLzma);

        await Eventually(() =>
        {
            Vm.Mods.Count.Should().Be(2);
            Vm.Mods.Should().Contain(ids.First());
        });

    }
    
    [Fact]
    public async Task CanDeleteMods()
    {
        await CreateLoadout();
        Vm.LoadoutId = Loadout.LoadoutId;
        
        var ids = new List<ModId>();
        for (var x = 0; x < 10; x++)
        {
            using var tx = Connection.BeginTransaction();
            var mod = new Mod.Model(tx)
            {
                Name = "Mod",
                Version = "1.0." + x,
                Enabled = true,
                Loadout = Loadout,
            };
            Loadout.Revise(tx);
            var result = await tx.Commit();
            ids.Add(ModId.From(result[mod.Id]));
        }

        await Eventually(() =>
        {
            Vm.Mods.Count.Should().Be(11, "because 10 mods were added, and we started with one");
        });

        var toDelete = Random.Shared.Next(1, 8);
        var toDeleteIds = ids.Take(toDelete).ToList();

        await Vm.DeleteMods(toDeleteIds, "Delete mods");

        await Eventually(() =>
        {

            Vm.Mods.Count.Should().Be(11 - toDelete, $"because {toDelete} mods were deleted of {10}");
            foreach (var id in toDeleteIds)
            {
                Vm.Mods.Any(m => m.Equals(id)).Should().BeFalse();
            }
        });
    }
}
