namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A simple loadout synchronizer that simply calls out to ALoadoutSynchronizer.
/// </summary>
public class DefaultSynchronizer : ALoadoutSynchronizer
{
    /// <summary>
    /// DI constructor
    /// </summary>
    public DefaultSynchronizer(IServiceProvider provider) : base(provider) { }
}
