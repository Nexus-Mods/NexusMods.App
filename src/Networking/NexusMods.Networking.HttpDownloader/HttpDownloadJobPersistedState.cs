using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.HttpDownloader;

[Include<PersistedJobState>]
public partial class HttpDownloadJobPersistedState : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.HttpDownloadJobPersistedState";

    /// <summary>
    /// The download URI.
    /// </summary>
    public static readonly UriAttribute Uri = new(Namespace, nameof(Uri));

    /// <inheritdoc cref="DownloadedFile.DownloadPageUri"/>
    public static readonly UriAttribute DownloadPageUri = new(Namespace, nameof(DownloadPageUri));

    /// <summary>
    /// The download destination.
    /// </summary>
    public static readonly AbsolutePathAttribute Destination = new(Namespace, nameof(Destination));
}
