using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.LoadoutSynchronizer;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public sealed class MountAndBlade2BannerlordLoadoutSynchronizer : ALoadoutSynchronizer
{
    public MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }

    public new ValueTask<ISortRule<Mod, ModId>[]> ModSortRules(Loadout loadout, Mod mod) => base.ModSortRules(loadout, mod);
    public new Task<IEnumerable<Mod>> SortMods(Loadout loadout) => base.SortMods(loadout);
}
