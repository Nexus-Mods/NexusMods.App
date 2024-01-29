using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public sealed class MountAndBlade2BannerlordLoadoutSynchronizer : ALoadoutSynchronizer
{
    public MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }

    public new ValueTask<ISortRule<Mod, ModId>[]> ModSortRules(Loadout loadout, Mod mod) => base.ModSortRules(loadout, mod);
    public new Task<IEnumerable<Mod>> SortMods(Loadout loadout) => base.SortMods(loadout);
}
