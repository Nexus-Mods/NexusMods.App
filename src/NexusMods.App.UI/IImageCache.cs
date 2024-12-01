using Avalonia.Media;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash3;
using OneOf;

namespace NexusMods.App.UI;

/// <summary>
/// Represents an image cache.
/// </summary>
[PublicAPI]
[Obsolete("To be replaced with resource pipelines")]
public interface IImageCache : IDisposable
{
    /// <summary>
    /// Gets an image from cache or loads the image.
    /// </summary>
    Task<IImage?> GetImage(ImageIdentifier imageIdentifier, CancellationToken cancellationToken);

    /// <summary>
    /// Prefetches the provided image.
    /// </summary>
    Task<Hash> Prefetch(ImageIdentifier imageIdentifier, CancellationToken cancellationToken);
}

public readonly struct ImageIdentifier
{
    public readonly OneOf<Uri, Hash> Union;

    public ImageIdentifier(OneOf<Uri, Hash> union)
    {
        Union = union;
    }
}
