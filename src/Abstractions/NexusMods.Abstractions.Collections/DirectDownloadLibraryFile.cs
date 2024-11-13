using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

/// <summary>
/// A direct downloaded file from a collection
/// </summary>
[Include<LibraryFile>]
public partial class DirectDownloadLibraryFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.Collections";

    /// <summary>
    /// The MD5 hash value of the downloaded file.
    /// </summary>
    public static readonly Md5Attribute Md5 = new(Namespace, nameof(Md5)) { IsIndexed = true };
    
    /// <summary>
    /// A user-friendly name of the file.
    /// </summary>
    public static readonly StringAttribute LogicalFileName = new(Namespace, nameof(LogicalFileName));
}
