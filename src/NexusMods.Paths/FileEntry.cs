namespace NexusMods.Paths;

/// <summary>
/// Represents an individual file on the filesystem.
/// </summary>
/// <param name="Path">Absolute path to the file in question.</param>
/// <param name="Size">The size of the file.</param>
/// <param name="LastModified">Last time this file was modified.</param>
public record FileEntry(AbsolutePath Path, Size Size, DateTime LastModified);
