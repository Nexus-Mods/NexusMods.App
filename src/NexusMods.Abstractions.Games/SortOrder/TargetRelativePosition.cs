namespace NexusMods.Abstractions.Games;

/// <summary>
/// The position items should be moved to, relative to the target item in ascending index order.
/// </summary>
public enum TargetRelativePosition
{
    /// <summary>
    /// Items should be moved to be before the target item in ascending index order
    /// </summary>
    BeforeTarget,
    
    /// <summary>
    /// Items should be moved to be after the target item in ascending index order
    /// </summary>
    AfterTarget,
}
