using NexusMods.Abstractions.MnemonicDB.Attributes;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// State specific to <see cref="HttpDownloadTask"/> suspend.
/// </summary>
// ReSharper disable once PartialTypeWithSinglePart
public static class HttpDownloadState
{
    private const string Namespace = "NexusMods.Networking.Downloaders.Tasks.State.HttpDownloadState";
    
    public static readonly UriAttribute Uri = new(Namespace, nameof(Uri));
};
