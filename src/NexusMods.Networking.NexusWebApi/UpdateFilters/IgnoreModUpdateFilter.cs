namespace NexusMods.Networking.NexusWebApi.UpdateFilters;

/// <summary>
/// A class whose purpose is to hide updates for <see cref="ModUpdateService"/>.
/// </summary>
public class IgnoreModUpdateFilter
{
    /// <summary>
    /// Plugs into <see cref="ModUpdateService.GetNewestFileVersionObservable"/>
    /// </summary>
    /// <param name="modPage">
    ///     The update info for the mod on a mod page.
    ///     We can either mutate the update info, or return 'null' to discard the update data.
    /// </param>
    /// <returns>The modified update data, or 'null' to discard the update data entirely</returns>
    public ModUpdateOnPage? SelectMod(ModUpdateOnPage modPage)
    {
        return null;
    }
    
    /// <summary>
    /// Plugs into <see cref="ModUpdateService.GetNewestModPageVersionObservable"/>
    /// </summary>
    /// <param name="modPage">
    ///     The update info for the mod page.
    ///     We can either mutate the update info, or return 'null' to discard the update data.
    /// </param>
    /// <returns>The modified update data, or 'null' to discard the update data entirely</returns>
    public ModUpdatesOnModPage? SelectModPage(ModUpdatesOnModPage modPage)
    {
        return null;
    }
    
}
