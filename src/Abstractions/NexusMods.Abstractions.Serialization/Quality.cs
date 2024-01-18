namespace NexusMods.Abstractions.Serialization;

/// <summary>
/// Used to indicate the quality of some information. Higher quality data should be used as
/// defaults over lower quality data.
/// </summary>
public enum Quality
{
    /// <summary>
    /// Highest level of quality.
    /// </summary>
    Highest = 0,
    /// <summary>
    /// Between <see cref="Highest"/> and <see cref="Normal"/> quality.
    /// </summary>
    High,
    /// <summary>
    /// Normal level of quality.
    /// </summary>
    Normal,
    /// <summary>
    /// Between <see cref="Normal"/> and <see cref="Low"/> quality.
    /// </summary>
    Low,
    /// <summary>
    /// Lowest level of quality.
    /// </summary>
    Lowest
}
