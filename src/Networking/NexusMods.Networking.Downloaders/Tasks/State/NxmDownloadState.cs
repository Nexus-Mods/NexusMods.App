using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.Downloaders.Tasks.State;

[Include<DownloaderState>]
public partial class NxmDownloadState : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.Downloaders.Tasks.State.NxmDownloadState";


    /// <summary>
    /// The mod id of the download task
    /// </summary>
    public static readonly ModIdAttribute ModId = new(Namespace, nameof(ModId)) { IsIndexed = true };
    
    /// <summary>
    /// The file id of the download task
    /// </summary>
    public static readonly FileIdAttribute FileId = new(Namespace, nameof(FileId)) { IsIndexed = true };
    
    /// <summary>
    /// The game domain of the download task
    /// </summary>
    public static readonly StringAttribute Game = new(Namespace, nameof(Game)) { IsIndexed = true };
    
    /// <summary>
    /// The expiry date of the download key
    /// </summary>
    public static readonly TimestampAttribute ValidUntil = new(Namespace, nameof(ValidUntil)) { IsOptional = true };

    /// <summary>
    /// The NXM key of the download task, used for free users and clicking "Download with manager"
    /// on the website
    /// </summary>
    public static readonly StringAttribute NxmKey = new(Namespace, nameof(NXMKey)) { IsOptional = true };
}
