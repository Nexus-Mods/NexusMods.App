namespace NexusMods.Games.TestFramework.Downloader;

/// <summary>
/// Defines the source this test mod is from.
/// </summary>
public enum RemoteModSource
{
    /// <summary>
    /// Sourced from the Nexus API.
    /// Corresponds to <see cref="NexusModMetadata"/> on deserialization.
    /// </summary>
    NexusMods,

    /// <summary>
    /// Sourced from the path relative to folder which is specified in the file.
    /// </summary>
    RealFileSystem,

    /*
      Other (future) alternative sources:
        - GitHub
        - GameBanana
        - ModDB
        etc.
    */
}
