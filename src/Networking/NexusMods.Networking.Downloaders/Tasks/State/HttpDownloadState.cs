using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Networking.Downloaders.Tasks.State;

/// <summary>
/// State specific to <see cref="HttpDownloadTask"/> suspend.
/// </summary>
/// <param name="Url">URL from which the item is to be downloaded.</param>
// ReSharper disable once PartialTypeWithSinglePart
[JsonName("NexusMods.Networking.Downloaders.Tasks.State.HttpDownloadState")]
public record HttpDownloadState (string? Url) : ITypeSpecificState
{
    // ReSharper disable once UnusedMember.Global - Required for serialization
    public HttpDownloadState() : this(string.Empty) { }
};
