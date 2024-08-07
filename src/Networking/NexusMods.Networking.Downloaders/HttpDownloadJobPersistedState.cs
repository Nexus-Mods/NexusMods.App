using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.Downloaders;

/// <summary>
/// The download state of a HTTP download job.
/// </summary>
[Include<PersistedJobState>]
public partial class HttpDownloadJobPersistedState : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.Downloaders.HttpDownloadJobPersistedState";

    /// <summary>
    /// The download sources
    /// </summary>
    public static readonly UriAttribute Uri = new(Namespace, nameof(Uri));
    
    /// <summary>
    /// The download destination
    /// </summary>
    public static readonly AbsolutePathAttribute Destination = new(Namespace, nameof(Destination));

}
