using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents a <see cref="LibraryItem"/> that is a file on disk.
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
    /// Size of the file.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
}
