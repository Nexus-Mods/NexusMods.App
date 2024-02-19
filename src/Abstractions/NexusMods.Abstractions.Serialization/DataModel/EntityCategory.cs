using NetEscapades.EnumGenerators;

namespace NexusMods.Abstractions.Serialization.DataModel;

/// <summary>
/// Represents a category associated with an entity.
///
/// Category is mostly used to determine which section/table of the datastore
/// (database) our information will be stored inside.
/// </summary>
/// <remarks>
///    Limited to 255 in our current implementation due to how we create IDs.
/// </remarks>
[EnumExtensions]
public enum EntityCategory : byte
{
    /// <summary>
    /// Represents roots/starting points of immutable lists.
    /// For more details, have a look at <a href="https://github.com/Nexus-Mods/NexusMods.App/blob/main/docs/ImmutableModlists.md">Immutable Mod Lists.</a>
    /// </summary>
    LoadoutRoots = 0,

    /// <summary>
    /// Loadouts, essentially 'profiles' for individual games.
    /// </summary>
    Loadouts = 1,

    /// <summary>
    /// For 'AModFile' and derivatives.
    /// </summary>
    ModFiles = 2,

    /// <summary>
    /// For 'Mod'(s).
    /// </summary>
    Mods = 3,

    /// <summary>
    /// Contains the info of analysing an individual file.
    ///
    /// Analyzed files contain information such as:<br/>
    ///  - Size of the File <br/>
    ///  - Hash of the File <br/>
    ///  - File/Archive Type 'FileType' <br/>
    /// </summary>
    FileAnalysis = 4,

    /// <summary>
    /// Used to store file hash entries.<br/>
    /// Hash entries consist of items which feature:<br/>
    ///  - <see cref="DateTime"/> Date<br/>
    ///  - Hash<br/>
    ///  - Size<br/>
    /// </summary>
    FileHashes = 5,

    /// <summary>
    /// Used for caching contents of archives. Such as size, hash and relative path. <br/>
    ///
    /// Example uses:<br/>
    ///     - Determining if an archive contains a file.
    /// </summary>
    FileContainedIn = 6,

    /// <summary>
    /// Used to store Nexus Authentication data [JWT Tokens and the like].
    /// </summary>
    AuthData = 7,

    /// <summary>
    /// Stores test information e.g. for mocking purposes.
    /// </summary>
    TestData = 8,

    /// <summary>
    /// This entity is used as part of the IPC job system.
    /// </summary>
    InterprocessJob = 9,

    /// <summary>
    /// Fingerprint cache data
    /// </summary>
    Fingerprints = 10,

    /// <summary>
    /// Back-indexes for finding hashes inside the ArchiveManager
    /// </summary>
    ArchivedFiles = 11,

    /// <summary>
    /// Metadata about archives, normally the source (e.g. NexusMods, GitHub, etc.)
    /// </summary>
    ArchiveMetaData = 12,

    /// <summary>
    /// Records for games that have been manually registered, as opposed to detected by
    /// the automatic locators
    /// </summary>
    ManuallyAddedGame = 13,

    /// <summary>
    /// Downloader resume/suspend state.
    /// </summary>
    DownloadStates = 15,

    /// <summary>
    /// Global settings for things like metrics opt-in and the like.
    /// </summary>
    GlobalSettings = 16,

    /// <summary>
    /// Information about registered downloads
    /// </summary>
    DownloadMetadata = 17,

    /// <summary>
    /// Disk state for loadouts
    /// </summary>
    DiskState = 18,

    /// <summary>
    /// Persisted workspaces.
    /// </summary>
    Workspaces = 19
}
