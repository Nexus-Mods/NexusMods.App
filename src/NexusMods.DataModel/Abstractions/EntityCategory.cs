using NetEscapades.EnumGenerators;
using NexusMods.DataModel.Attributes;
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
public enum EntityCategory
{
    /// <summary>
    /// Loadouts, essentially 'profiles' for individual games.
    /// </summary>
    [Immutable]
    Loadouts,

    /// <summary>
    /// Contains the info of analysing an individual file.
    ///
    /// Analyzed files contain information such as:<br/>
    ///  - <see cref="Size"/> of the File <br/>
    ///  - <see cref="Hash"/> of the File <br/>
    ///  - File/Archive Type (<see cref="FileType"/>) <br/>
    /// </summary>
    FileAnalysis,

    /// <summary>
    /// Represents roots/starting points of immutable lists.
    /// For more details, have a look at <a href="https://github.com/Nexus-Mods/NexusMods.App/blob/main/docs/ImmutableModlists.md">Immutable Mod Lists.</a>
    /// </summary>
    LoadoutRoots,

    /// <summary>
    /// Used to store file hash entries.<br/>
    /// Hash entries consist of items which feature:<br/>
    ///  - <see cref="DateTime"/> Date<br/>
    ///  - <see cref="Hash"/> Hash<br/>
    ///  - <see cref="Size"/> Size<br/>
    /// </summary>
    FileHashes,

    /// <summary>
    /// Used for caching contents of archives. Such as size, hash and relative path. <br/>
    ///
    /// Example uses:<br/>
    ///     - Determining if an archive contains a file.
    /// </summary>
    FileContainedIn,

    /// <summary>
    /// Used to store Nexus Authentication data [JWT Tokens and the like].
    /// </summary>
    AuthData,

    /// <summary>
    /// Stores test information e.g. for mocking purposes.
    /// </summary>
    TestData,
    
    /// <summary>
    /// This entity is used as part of the IPC job system.
    /// </summary>
    InterprocessJob,
    
    /// <summary>
    /// Fingerprint cache data
    /// </summary>
    Fingerprints,
    
    // TEMP TODO: Remove this
    FileContainedInEx,
}
