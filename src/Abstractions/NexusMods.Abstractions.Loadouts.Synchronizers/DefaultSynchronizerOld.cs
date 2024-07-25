namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A simple loadout synchronizer that simply calls out to ALoadoutSynchronizer.
/// </summary>
public class DefaultSynchronizerOld : ALoadoutSynchronizerOld
{
    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="provider"></param>
    public DefaultSynchronizerOld(IServiceProvider provider) : base(provider) { }
}
