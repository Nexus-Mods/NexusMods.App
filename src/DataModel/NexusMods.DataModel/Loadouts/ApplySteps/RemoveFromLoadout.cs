using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// This step removes a file from all mods in a given loadout.
/// </summary>
public record RemoveFromLoadout : IApplyStep
{
    /// <summary>
    /// Path of the item to remove.
    /// </summary>
    public required AbsolutePath To { get; init; }
}
