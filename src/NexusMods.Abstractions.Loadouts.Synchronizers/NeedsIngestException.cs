namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Thrown when there are changes in the game folder that need to be ingested before a loadout can be applied
/// </summary>
public class NeedsIngestException : Exception
{
    /// <summary>
    /// Thrown when a loadout needs to be ingested before it can be applied
    /// </summary>
    public NeedsIngestException() : base("Loadout needs to be ingested before it can be applied")
    {

    }
}
