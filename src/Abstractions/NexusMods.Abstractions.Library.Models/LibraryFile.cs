using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.Models;

/// <summary>
/// Represents a <see cref="LibraryItem"/> that is a file in the library.
/// </summary>
[PublicAPI]
[Include<LibraryItem>]
public partial class LibraryFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LibraryFile";

    /// <summary>
    /// Hash of the file.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { IsIndexed = true };

    /// <summary>
    /// MD5 hash for compatibility with bad systems.
    /// </summary>
    public static readonly Md5Attribute Md5 = new(Namespace, nameof(Md5)) { IsIndexed = true, IsOptional = true };

    /// <summary>
    /// Size of the file.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));

    /// <summary>
    /// Name of the file.
    /// </summary>
    public static readonly RelativePathAttribute FileName = new(Namespace, nameof(FileName));
}
