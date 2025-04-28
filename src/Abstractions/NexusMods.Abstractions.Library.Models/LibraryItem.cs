using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.Cascade;
using NexusMods.Cascade.Rules;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Cascade;
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
        
        /// <summary>
        /// Tries to get the entity as a DownloadedFile entity, if the entity is not a DownloadedFile entity, it returns false.
        /// </summary>
        public bool TryGetAsDownloadedFile(out DownloadedFile.ReadOnly result) {
            // This is same as source generator would make, just that we don't
            // currently don't autogenerate conversions for multiple levels of include.
            var casted = new DownloadedFile.ReadOnly(Db, EntitySegment, Id);
            if (casted.IsValid()) {
                result = casted;
                return true;
            }

            result = default(DownloadedFile.ReadOnly);
            return false;
        }
        
        /// <summary>
        /// Tries to get the entity as a LocalFile entity, if the entity is not a LocalFile entity, it returns false.
        /// </summary>
        public bool TryGetAsLocalFile(out LocalFile.ReadOnly result) {
            // This is same as source generator would make, just that we don't
            // currently don't autogenerate conversions for multiple levels of include.
            var casted = new LocalFile.ReadOnly(Db, EntitySegment, Id);
            if (casted.IsValid()) {
                result = casted;
                return true;
            }

            result = default(LocalFile.ReadOnly);
            return false;
        }
    }
}
