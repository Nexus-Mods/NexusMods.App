using Avalonia.Media;
using NexusMods.Abstractions.GuidedInstallers;

namespace NexusMods.App.UI;

/// <summary>
/// Image cache.
/// </summary>
public interface IImageCache : IDisposable
{
    /// <summary>
    /// Gets an image from cache or loads the image.
    /// </summary>
    Task<IImage?> GetImage(OptionImage optionImage, CancellationToken cancellationToken);
}
