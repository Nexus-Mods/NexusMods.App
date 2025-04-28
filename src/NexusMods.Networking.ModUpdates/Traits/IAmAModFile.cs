namespace NexusMods.Networking.ModUpdates.Traits;

/// <summary>
/// Specifies the minimum required interface to represent a mod file within a mod page.
/// This API provides the minimum required 
/// </summary>
public interface IAmAModFile
{
    /// <summary>
    /// The name of the mod file. (File Name)
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The version of the mod file. (File Version)
    /// </summary>
    /// <remarks>
    /// The Nexus mod site allows arbitrary input here, so this is a string.
    /// </remarks>
    public string Version { get; }

    /// <summary>
    /// When the file was uploaded to Nexus Mods
    /// </summary>
    public DateTimeOffset UploadedAt { get; }

    /// <summary>
    /// Returns all other files the same mod page.
    /// </summary>
    public IEnumerable<IAmAModFile> OtherFilesInSameModPage { get; }
}
