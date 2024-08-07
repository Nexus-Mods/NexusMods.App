using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Networking.HttpDownloader;

namespace NexusMods.Networking.NexusWebApi;

[Include<HttpDownloadJobPersistedState>]
public partial class NexusModsDownloadJobPersistedState : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.NexusWebApi";

    public static readonly ReferenceAttribute<NexusModsFileMetadata> FileMetadata = new(Namespace, nameof(FileMetadata));
}
