using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Library.Models;

/// <summary>
/// Represents an item in the library.
/// </summary>
[PublicAPI]
public partial class LibraryItem : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.LibraryItem";

    /// <summary>
    /// Name of the library item.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
}
