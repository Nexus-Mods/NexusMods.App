using NexusMods.DataModel.Attributes;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.DataModel.ArchiveContents;

/// <summary>
/// Represents a container for archived files. 
/// </summary>
public static class ArchivedFileContainer
{
    private const string Namespace = "NexusMods.DataModel.ArchiveContents.ArchivedFileContainer";
    
    /// <summary>
    /// The name of the container on-disk. This will be relative to some archive root path.
    /// </summary>
    public static readonly RelativePathAttribute Path = new(Namespace, nameof(Path));

    /// <summary>
    /// Model for the archived file container.
    /// </summary>
    /// <param name="tx"></param>
    public class Model(ITransaction tx) : AEntity(tx)
    {
        /// <summary>
        /// The name of the container on-disk. This will be relative to some archive root path.
        /// </summary>
        public RelativePath Path
        {
            get => ArchivedFileContainer.Path.Get(this);
            set => ArchivedFileContainer.Path.Add(this, value);
        }

        /// <summary>
        /// The file entries contained in this container.
        /// </summary>
        public Entities<EntityIds, ArchivedFile.Model> Contents 
            => GetReverse<ArchivedFile.Model>(ArchivedFile.Container);
    }
    
}
