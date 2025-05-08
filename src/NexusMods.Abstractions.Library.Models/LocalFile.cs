using JetBrains.Annotations;
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
}
