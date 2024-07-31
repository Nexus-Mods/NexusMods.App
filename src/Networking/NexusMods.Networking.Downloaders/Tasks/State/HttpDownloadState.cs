using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// State specific to <see cref="HttpDownloadTask"/> suspend.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
[Include<DownloaderState>]
[Obsolete("To be replaced by jobs")]
public partial class HttpDownloadState : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.Downloaders.Tasks.State.HttpDownloadState";
    
    public static readonly UriAttribute Uri = new(Namespace, nameof(Uri));
};
