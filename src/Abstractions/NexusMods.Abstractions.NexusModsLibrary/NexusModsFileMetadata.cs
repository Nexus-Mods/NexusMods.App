using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Abstractions.Telemetry;
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
    /// Unique identifier for the file on Nexus Mods.
    /// </summary>
    public static readonly UidForFileAttribute Uid = new(Namespace, nameof(Uid)) { IsIndexed = true };

    /// <summary>
    /// The name of the file.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The version of the file.
    /// </summary>
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));

    /// <summary>
    /// The date the file was uploaded at.
    /// </summary>
    public static readonly DateTimeAttribute UploadedAt = new(Namespace, nameof(UploadedAt));
    
    /// <summary>
    /// The size in bytes of the file.
    /// </summary>
    public static readonly SizeAttribute Size = new(Namespace, nameof(Size)) { IsOptional = true };

    /// <summary>
    /// Reference to the mod page of the file.
    /// </summary>
    public static readonly ReferenceAttribute<NexusModsModPageMetadata> ModPage = new(Namespace, nameof(ModPage));
    
    /// <summary>
    /// Library Files that link to this file.
    /// </summary>
    public static readonly BackReferenceAttribute<NexusModsLibraryItem> LibraryFiles = new(NexusModsLibraryItem.FileMetadata);

    public partial struct ReadOnly
    {
        public Uri GetUri()
        {
            // NOTE(erri120): This URI shows a single download button for the exact file
            // The nmm=1 turns the button into a nxm:// link, without nmm=1 the button will download the file through the browser
            // Example: https://www.nexusmods.com/stardewvalley/mods/29140?tab=files&file_id=115276&nmm=1
            return NexusModsUrlBuilder.CreateCollectionsUri($"{ModPage.GetBaseUrl()}?tab=files&file_id={Uid.FileId}&nmm=1");
        }
    }
}
