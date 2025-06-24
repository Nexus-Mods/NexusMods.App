using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.Models;

/// <summary>
/// Represents a file inside a library archive.
/// </summary>
/// <remarks>
///     Please do not create alternatives to LibraryArchiveFileEntry that can store
///     archived files without updating the GC.
/// </remarks>
[PublicAPI]
[Include<LibraryFile>]
public partial class LibraryArchiveFileEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LibraryArchiveFileEntry";

    /// <summary>
    /// Path to the file inside the archive.
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path));

    /// <summary>
    /// Reference to the parent archive that contains this file.
    /// </summary>
    public static readonly ReferenceAttribute<LibraryArchive> Parent = new(Namespace, nameof(Parent));
}
