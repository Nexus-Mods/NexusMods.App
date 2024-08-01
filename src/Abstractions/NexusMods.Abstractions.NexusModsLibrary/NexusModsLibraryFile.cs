using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represented a <see cref="DownloadedFile"/> originating from Nexus Mods.
/// </summary>
[PublicAPI]
[Include<DownloadedFile>]
public partial class NexusModsLibraryFile : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NexusModsLibraryFile";

    /// <summary>
    /// Remote metadata of the file on Nexus Mods.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsFileMetadata> FileMetadata = new(Namespace, nameof(FileMetadata));
}
