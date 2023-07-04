using NetEscapades.EnumGenerators;
using NexusMods.DataModel.Attributes;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Abstractions;

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
    /// For <see cref="AModFile"/>.
    /// </summary>
    ModFiles = 2,

    /// <summary>
    /// For <see cref="Mod"/>.
    /// </summary>
    Mods = 3,

    /// <summary>
    /// Contains the info of analysing an individual file.
    ///
    /// Analyzed files contain information such as:<br/>
    ///  - <see cref="Size"/> of the File <br/>
    ///  - <see cref="Hash"/> of the File <br/>
    ///  - File/Archive Type (<see cref="FileType"/>) <br/>
    /// </summary>
    FileAnalysis = 4,

    /// <summary>
    /// Used to store file hash entries.<br/>
    /// Hash entries consist of items which feature:<br/>
    ///  - <see cref="DateTime"/> Date<br/>
    ///  - <see cref="Hash"/> Hash<br/>
    ///  - <see cref="Size"/> Size<br/>
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
    /// See <see cref="Diagnostics"/>.
    /// </summary>
    Diagnostics = 14,
}
