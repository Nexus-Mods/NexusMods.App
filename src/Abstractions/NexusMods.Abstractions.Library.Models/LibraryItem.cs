using System.ComponentModel;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
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

public partial class LibraryItem
{
    [PublicAPI]
    public partial struct ReadOnly
    {
        /// <summary>
        /// Adds a retraction which effectively deletes the current archived file from the data store.
        /// </summary>
        /// <param name="tx">The transaction to add the retraction to.</param>
        public void Retract(ITransaction tx) => tx.Retract(Id, LibraryItem.Name, Name);
    }
}
