using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using OneOf;

namespace NexusMods.App.UI;

/// <summary>
/// Optimized storage for images.
/// </summary>
[PublicAPI]
public interface IImageStore
{
    ValueTask<StoredImage.ReadOnly> PutAsync(Bitmap bitmap);

    Bitmap? Get(OneOf<StoredImageId, StoredImage.ReadOnly> input);
}
