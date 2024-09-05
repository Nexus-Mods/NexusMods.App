using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents a NXM download.
/// </summary>
[PublicAPI]
public partial class NXMDownloadState : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NXMDownloadState";

    /// <summary>
    /// Metadata about the file on nexusmods.com.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsFileMetadata> FileMetadata = new(Namespace, nameof(FileMetadata));
}
