using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts.Files;

/// <summary>
/// A mod file that is stored in the IFileStore. In other words,
/// this file is not generated on-the-fly or contain any sort of special
/// logic that defines its contents. Because of this we know the hash
/// and the size. This file may originally come from a download, a
/// tool's output, or a backed up game file.
/// </summary>
[Include<File>]
[Obsolete(message: "This will be replaced with `LoadoutFile`")]
public partial class StoredFile : IModelDefinition
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
}
