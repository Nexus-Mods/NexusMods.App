namespace NexusMods.DataModel.Games.GameCapabilities.FomodCustomInstallPathCapability;

/// <summary>
/// A collection of <see cref="IGameCapability"/> instances.
/// </summary>
public class GameCapabilityCollection : Dictionary<GameCapabilityId, IGameCapability>
{

    /// <summary>
    /// Tries to get the capability with the specified Id.
    /// </summary>
    /// <param name="capabilityId"> The id of the capability to retrieve</param>
    /// <param name="outCapability"> The obtained capability if found, null if missing</param>
    /// <typeparam name="TCapability"> The type of capability to be retrieved</typeparam>
    /// <returns>true if present, false if not</returns>
    public bool TryGetCapability<TCapability>(GameCapabilityId capabilityId, out TCapability? outCapability)
        where TCapability : class, IGameCapability
    {
        if (TryGetValue(capabilityId, out var capability))
        {
            if (capability is TCapability typedCapability)
            {
                outCapability = typedCapability;
                return true;
            }
        }

        outCapability = null;
        return false;
    }

    /// <summary>
    /// Tries to get the capability with the specified Id.
    /// </summary>
    /// <param name="capabilityId"> The id of the capability to retrieve</param>
    /// <typeparam name="TCapability"> The type of capability to be retrieved</typeparam>
    /// <returns>The found Capability or null if missing</returns>
    public TCapability? GetByIdOrDefault<TCapability>(GameCapabilityId capabilityId)
        where TCapability : class, IGameCapability
    {
        if (TryGetValue(capabilityId, out var capability))
        {
            if (capability is TCapability typedCapability)
            {
                return typedCapability;
            }
        }
        return default;
    }

    /// <summary>
    /// Adds a new capability implementation
    /// NOTE: If a capability with the same Id already exists, it will be overwritten.
    /// </summary>
    /// <param name="id">Id of the capability type to be added</param>
    /// <param name="capability">Concrete capability implementation</param>
    /// <returns>This collection</returns>
    public GameCapabilityCollection Register(GameCapabilityId id, IGameCapability capability)
    {
        this[id] = capability;
        return this;
    }
}
