using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public sealed class MountAndBlade2BannerlordLoadoutSynchronizer : ALoadoutSynchronizer
{
    public MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }

    public new ValueTask<ISortRule<Mod, ModId>[]> ModSortRules(Loadout loadout, Mod mod) => base.ModSortRules(loadout, mod);
    public new Task<IEnumerable<Mod>> SortMods(Loadout loadout) => base.SortMods(loadout);
}
