﻿namespace NexusMods.DataModel.LoadoutSynchronizer;

/// <summary>
/// A simple loadout synchronizer that simply calls out to ALoadoutSynchronizer.
/// </summary>
public class DefaultSynchronizer : ALoadoutSynchronizer
{
    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="provider"></param>
    public DefaultSynchronizer(IServiceProvider provider) : base(provider) { }
}
