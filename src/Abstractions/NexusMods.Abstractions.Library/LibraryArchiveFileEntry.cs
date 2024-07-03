using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents a file inside a library archive.
/// </summary>
[PublicAPI]
public partial class LibraryArchiveFileEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LibraryArchiveFileEntry";

    /// <summary>
    /// Reference to the parent archive that contains this file.
    /// </summary>
    public static readonly ReferenceAttribute<LibraryArchive> Parent = new(Namespace, nameof(Parent));

    /// <summary>
    /// Hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { IsIndexed = true };

    /// <summary>
    /// Size of the file.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));

    /// <summary>
    /// Name of the file.
    /// </summary>
    public static readonly RelativePathAttribute FileName = new(Namespace, nameof(FileName));
}

