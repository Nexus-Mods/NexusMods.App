namespace NexusMods.Abstractions.Loadouts.Files.Diff;

/// <summary>
/// The type of change of a file at a specific path
/// </summary>
public enum FileChangeType : byte
{
    /// <summary>
    /// No change
    /// </summary>
    None,
    
    /// <summary>
    /// This file path was removed and was present before
    /// </summary>
    Removed,
    
    /// <summary>
    /// The contents of the file at this path were modified, hash is different
    /// </summary>
    /// <remarks>
    /// A renamed file is not considered modified, but rather a Removed and an Added file
    /// </remarks>
    Modified,
    
    /// <summary>
    /// This file path was added and was not present before
    /// </summary>
    Added,
}
