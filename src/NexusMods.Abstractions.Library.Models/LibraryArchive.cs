using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.Models;

/// <summary>
/// Represents an archive in the library.
/// </summary>
[PublicAPI]
[Include<LibraryFile>]
public partial class LibraryArchive : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LibraryArchive";

    /// <summary>
    /// Marker.
    /// </summary>
    public static readonly MarkerAttribute Archive = new(Namespace, nameof(Archive));

    /// <summary>
    /// Back-reference to all files inside the archive.
    /// </summary>
    public static readonly BackReferenceAttribute<LibraryArchiveFileEntry> Children = new(LibraryArchiveFileEntry.Parent);
}
