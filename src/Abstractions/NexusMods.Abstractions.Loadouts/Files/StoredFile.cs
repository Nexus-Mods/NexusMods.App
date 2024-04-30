using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// A mod file that is stored in the IFileStore. In other words,
/// this file is not generated on-the-fly or contain any sort of special
/// logic that defines its contents. Because of this we know the hash
/// and the size. This file may originally come from a download, a
/// tool's output, or a backed up game file.
/// </summary>
public static class  StoredFile
{
    private const string Namespace = "NexusMods.Abstractions.Loadouts.Files.StoredFile";

    /// <summary>
    /// The size of the file, on disk after extraction.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size));
    
    /// <summary>
    /// The hash of the file, on disk after extraction.
    /// </summary>
    public static readonly HashAttribute Hash = new(Namespace, nameof(Hash)) { IsIndexed = true };
    
    /// <summary>
    /// The location the file will be installed to
    /// </summary>
    public static readonly GamePathAttribute To = new(Namespace, nameof(To));


    /// <summary>
    /// Model for the stored file.
    /// </summary>
    public class Model(ITransaction tx) : File.Model(tx)
    {

        /// <summary>
        /// The size of the file.
        /// </summary>
        public Size Size
        {
            get => StoredFile.Size.Get(this);
            set => StoredFile.Size.Add(this, value);
        }
        
        /// <summary>
        /// The hash of the file.
        /// </summary>
        public Hash Hash
        {
            get => StoredFile.Hash.Get(this);
            set => StoredFile.Hash.Add(this, value);
        }
        
        /// <summary>
        /// The location the file will be installed to
        /// </summary>
        public GamePath To
        {
            get => StoredFile.To.Get(this);
            set => StoredFile.To.Add(this, value);
        }
        
    }
}
