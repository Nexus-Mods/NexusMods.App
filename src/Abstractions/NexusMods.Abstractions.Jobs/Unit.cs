namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// A return value for a job that returns nothing
/// </summary>
public struct Unit
{
    /// <summary>
    /// A singleton instance of <see cref="Unit"/>
    /// </summary>
    public static Unit Instance => default;
}
