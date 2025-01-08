using JetBrains.Annotations;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary.Models;

/// <summary>
/// <see cref="CollectionDownload"/> for a file on Nexus Mods.
/// </summary>
/// <remarks>
/// Source = `nexus` in the collection JSON file.
/// </remarks>
[PublicAPI]
[Include<CollectionDownload>]
public partial class CollectionDownloadNexusMods : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.CollectionDownloadNexusMods";

    /// <summary>
    /// <see cref="UidForFile"/>.
    /// </summary>
    public static readonly UidForFileAttribute FileUid = new(Namespace, nameof(FileUid)) { IsIndexed = true };

    /// <summary>
    /// <see cref="UidForMod"/>.
    /// </summary>
    public static readonly UidForModAttribute ModUid = new(Namespace, nameof(ModUid)) { IsIndexed = true };

    /// <summary>
    /// Reference to the file metadata.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsFileMetadata> FileMetadata = new(Namespace, nameof(FileMetadata));
}
