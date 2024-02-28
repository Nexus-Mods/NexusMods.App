using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Singleton service for applying loadouts and tracking what loadouts are currently applied/being applied.
/// </summary>
public interface IApplyService
{
    /// <summary>
    /// Apply a loadout to its game installation.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public Task<Loadout> Apply(LoadoutId loadoutId);


    /// <summary>
    /// Returns the last applied loadout for a given game installation.
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns>A tuple of the LoadoutId and loadout revision Id of the last applied state</returns>
    public (LoadoutId, IId) GetLastAppliedLoadout(GameInstallation gameInstallation);
}
