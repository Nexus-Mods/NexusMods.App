using FluentAssertions;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.App.UI.RightContent.LoadoutGrid;

namespace NexusMods.UI.Tests.RightContent;

public class LoadoutGridViewModelTests : AVmTest<ILoadoutGridViewModel>
{
    public LoadoutGridViewModelTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task CanDeleteMods()
    {
        Vm.LoadoutId = Loadout.Value.LoadoutId;

        var ids = new List<ModId>();
        for (int x = 0; x < 10; x++)
        {
            var id = ModId.NewId();
            ids.Add(id);
            Loadout.Add(new Mod
            {
                Id = id,
                Name = $"Mod {x}",
                Version = "1.0.0",
                Enabled = true,
                Files = new EntityDictionary<ModFileId, AModFile>(DataStore)
            });
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
                Vm.Mods.Any(m => m.ModId.Equals(id)).Should().BeFalse();
            }
        });
    }

    [Fact]
    public async Task AddingModUpdatesTheModSource()
    {
        Vm.LoadoutId = Loadout.Value.LoadoutId;

        var ids = await InstallMod(DataZipLzma);

        await Eventually(() =>
        {
            Vm.Mods.Count.Should().Be(2);
            Vm.Mods.Should().Contain(new ModCursor(Loadout.Value.LoadoutId, ids.First()));
        });

    }
}
