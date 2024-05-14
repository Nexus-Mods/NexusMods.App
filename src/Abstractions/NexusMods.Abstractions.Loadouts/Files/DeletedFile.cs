using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.MnemonicDB.Attributes.Extensions;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// Deleted files in the app are reified, that is to say they exist as a item in the datamodel. This
/// is to allow for tracking of deletes on disk without having to modify mods or files directly. So Mod
/// A may provide a file A, and then Mod B may also provide file A, and then in the `Overrides` A may also
/// exist, but marked as deleted. This means the mods can remain unmodified, and the overrides can be
/// the final say on what is installed.
/// </summary>
public static class DeletedFile
{
    public const string Namespace = "NexusMods.Abstractions.Loadouts.Files.DeletedFile";
    
    /// <summary>
    /// If set to true, the file is considered deleted.
    /// </summary>
    public static readonly MarkerAttribute Deleted = new(Namespace, nameof(Deleted));

    /// <summary>
    /// Model for a deleted file.
    /// </summary>
    public class Model(ITransaction tx) : File.Model(tx)
    {
        /// <summary>
        /// True if the file is deleted.
        /// </summary>
        public bool Deleted
        {
            get => DeletedFile.Deleted.Contains(this);
            set => DeletedFile.Deleted.Add(this);
        }
    }
    
    
    /// <summary>
    /// If this file is a deleted file, this will return true and cast the deleted file to the out parameter.
    /// </summary>
    public static bool TryGetAsDeletedFile(this File.Model model, [NotNullWhen(true)] out Model? deletedFile)
    {
        deletedFile = null;
        if (!model.Contains(Deleted))
            return false;

        deletedFile = model.Remap<Model>();
        return true;
    }
}
