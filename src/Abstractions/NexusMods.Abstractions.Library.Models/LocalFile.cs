using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.Models;

/// <summary>
/// Represents a local file in the library.
/// </summary>
[PublicAPI]
[Include<LibraryFile>]
public partial class LocalFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LocalFile";

    /// <summary>
    /// The original path from where the local file originated from.
    /// </summary>
    public static readonly StringAttribute OriginalPath = new(Namespace, nameof(OriginalPath));

    /// <summary>
    /// The MD5 hash value of the file.
    /// </summary>
    public static readonly Md5Attribute Md5 = new(Namespace, nameof(Md5)) { IsOptional = true };
}
