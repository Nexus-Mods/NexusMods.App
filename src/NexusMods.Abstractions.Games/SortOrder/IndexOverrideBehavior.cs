namespace NexusMods.Abstractions.Games;

/// <summary>
/// Defines whether smaller or greater index numbers win in case of conflicts between items in sorting order
/// </summary>
public enum IndexOverrideBehavior
{
    /// <summary>
    /// Items with Smaller index numbers win in case of conflicts with greater index number items 
    /// </summary>
    SmallerIndexWins,

    /// <summary>
    /// Items with Greater index numbers win in case of conflicts with smaller index number items
    /// </summary>
    GreaterIndexWins,
}
