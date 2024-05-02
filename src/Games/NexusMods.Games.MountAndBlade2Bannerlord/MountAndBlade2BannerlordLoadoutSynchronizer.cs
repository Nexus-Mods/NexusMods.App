using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public sealed class MountAndBlade2BannerlordLoadoutSynchronizer : ALoadoutSynchronizer
{
    public MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }

    public new ValueTask<ISortRule<Mod.Model, ModId>[]> ModSortRules(Loadout.Model loadout, Mod.Model mod) => base.ModSortRules(loadout, mod);
    public new Task<IEnumerable<Mod.Model>> SortMods(Loadout.Model loadout) => base.SortMods(loadout);
}
