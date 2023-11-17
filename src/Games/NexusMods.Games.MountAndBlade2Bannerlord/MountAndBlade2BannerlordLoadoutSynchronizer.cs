using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.LoadoutSynchronizer;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public sealed class MountAndBlade2BannerlordLoadoutSynchronizer : ALoadoutSynchronizer
{
    public MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : base(provider) { }

    public new Task<IEnumerable<Mod>> SortMods(Loadout loadout) => base.SortMods(loadout);
}
