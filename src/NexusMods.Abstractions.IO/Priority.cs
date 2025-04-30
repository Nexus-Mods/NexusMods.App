#pragma warning disable CS1591
namespace NexusMods.Abstractions.IO;

/// <summary>
/// Generic enum for expressing the priority of a given operation. Most operations will be Normal priority, but
/// with more specific operations being given higher priority, and those that should be used as a last resort being
/// given lower priority. Use Priority.None for operations that should not be used at all.
/// </summary>
public enum Priority
{
    Highest = 0,
    High,
    Normal,
    Low,
    Lowest,
    None = int.MaxValue
}
