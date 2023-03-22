using JetBrains.Annotations;

namespace NexusMods.Paths;

/// <summary>
/// Enum for known paths to folders and files.
/// </summary>
[PublicAPI]
public enum KnownPath
{
    /// <summary>
    /// The path of the base directory that the assembly
    /// resolver uses to probe for assemblies.
    /// </summary>
    /// <remarks>
    /// This is often the same as <see cref="CurrentDirectory"/>.
    /// </remarks>
    EntryDirectory,

    /// <summary>
    /// The current working directory.
    /// </summary>
    /// <remarks>
    /// This is often the same as <see cref="EntryDirectory"/>.
    /// </remarks>
    CurrentDirectory,

    /// <summary>
    /// The current user's temporary folder.
    /// </summary>
    TempDirectory,

    /// <summary>
    /// The user's profile folder.
    /// </summary>
    /// <remarks>
    /// On Windows, this folder is located at "%USERPROFILE%"
    /// ("%SystemDrive%\Users\%USERNAME%"). On Linux, this folder
    /// is located at "$HOME" ("/home/$USER").
    /// </remarks>
    HomeDirectory,

    /// <summary>
    /// Documents (My Documents) folder. On Windows, this folder is
    /// located at "%USERPROFILE%\Documents". On Linux, this folder
    /// is located at "$XDG_DOCUMENTS_DIR" ("/home/$USER/Documents").
    /// </summary>
    MyDocumentsDirectory,

    /// <summary>
    /// The <c>My Games</c> folder, relative to <see cref="HomeDirectory"/>.
    /// Note: While many games like to use this folder, it is not a special
    /// folder and doesn't have a CSID.
    /// </summary>
    MyGamesDirectory,
}
