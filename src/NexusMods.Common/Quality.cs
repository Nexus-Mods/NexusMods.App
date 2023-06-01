namespace NexusMods.Common;

/// <summary>
/// Used to indicate the quality of some information. Higher quality data should be used as
/// defaults over lower quality data.
/// </summary>
public enum Quality
{
    Highest = 0,
    High,
    Normal,
    Low,
    Lowest
}
