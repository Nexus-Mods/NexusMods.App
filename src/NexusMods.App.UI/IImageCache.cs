using Avalonia.Media;
using JetBrains.Annotations;
using NexusMods.Hashing.xxHash64;
using OneOf;

namespace NexusMods.App.UI;

/// <summary>
/// Represents an image cache.
/// </summary>
[PublicAPI]
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
