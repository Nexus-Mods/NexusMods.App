namespace NexusMods.Abstractions.NexusWebApi.Types;

/// <summary>
/// Specifies the time period used to search for items.
/// </summary>
public enum PastTime
{
    /// <summary>
    /// Searches the past 24 hours.
    /// </summary>
    Day,

    /// <summary>
    /// Searches the past 7 days.
    /// </summary>
    Week,

    /// <summary>
    /// Searches the past month.
    /// </summary>
    Month,
}
