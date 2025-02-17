using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

/// <summary>
/// A direct downloaded file from a collection
/// </summary>
[Include<LocalFile>]
public partial class DirectDownloadLibraryFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.DirectDownloadLibraryFile";

    /// <summary>
    /// A user-friendly name of the file.
    /// </summary>
    public static readonly StringAttribute LogicalFileName = new(Namespace, nameof(LogicalFileName));
}
