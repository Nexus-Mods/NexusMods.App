using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.TransformerHooks.BeforeSort.Builtin;

/// <summary>
/// Removes mods from the sort which are disabled.
/// </summary>
public class DisableDisabledMods : IBeforeSort
{
    public IEnumerable<GameDomain> GameDomains { get; } = Array.Empty<GameDomain>();
    public ValueTask<Result> BeforeSortAsync(Mod mod, Loadout loadout, CancellationToken ct)
    {
        return !mod.Enabled ? 
            new ValueTask<Result>(Result.DisableMod) : 
            new ValueTask<Result>(Result.Nothing);
    }
}
