using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents a file inside a library archive.
/// </summary>
[PublicAPI]
[Include<LibraryFile>]
public partial class LibraryArchiveFileEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LibraryArchiveFileEntry";

    /// <summary>
    /// Reference to the parent archive that contains this file.
    /// </summary>
    public static readonly ReferenceAttribute<LibraryArchive> Parent = new(Namespace, nameof(Parent));
}
