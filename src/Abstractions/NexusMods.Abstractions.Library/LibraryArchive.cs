using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents an archive in the library.
/// </summary>
[PublicAPI]
public partial class LibraryArchive : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LibraryArchive";

    /// <summary>
    /// Reference to the actual file in the library.
    /// </summary>
    public static readonly ReferenceAttribute<LibraryFile> LibraryFile = new(Namespace, nameof(LibraryFile));

    /// <summary>
    /// Back-reference to all files inside the archive.
    /// </summary>
    public static readonly BackReferenceAttribute<LibraryArchiveFileEntry> Children = new(LibraryArchiveFileEntry.Parent);
}
