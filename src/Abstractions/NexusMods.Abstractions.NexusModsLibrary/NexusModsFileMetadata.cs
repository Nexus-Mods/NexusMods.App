using JetBrains.Annotations;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents a remote file on Nexus Mods.
/// </summary>
[PublicAPI]
public partial class NexusModsFileMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Library.NexusModsFileMetadata";

    /// <summary>
    /// The ID of the file.
    /// </summary>
    public static readonly FileIdAttribute FileId = new(Namespace, nameof(FileId)) { IsIndexed = true };

    /// <summary>
    /// The name of the file.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The version of the file.
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));

    /// <summary>
    /// Reference to the mod page of the file.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsModPageMetadata> ModPage = new(Namespace, nameof(ModPage));
    
    /// <summary>
    /// Library Files that link to this file.
    /// </summary>
    public static readonly BackReferenceAttribute<NexusModsLibraryFile> LibraryFiles = new(NexusModsLibraryFile.FileMetadata);
}
