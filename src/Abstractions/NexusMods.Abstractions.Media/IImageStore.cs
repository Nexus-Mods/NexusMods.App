using Avalonia.Media.Imaging;
using BitFaster.Caching;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using OneOf;

namespace NexusMods.Abstractions.Media;

/// <summary>
/// Optimized storage for images.
/// </summary>
[PublicAPI]
public interface IImageStore
{
    ValueTask<StoredImage.ReadOnly> PutAsync(Bitmap bitmap);

    [MustDisposeResource] Lifetime<Bitmap>? Get(OneOf<StoredImageId, StoredImage.ReadOnly> input);

    StoredImage.New CreateStoredImage(ITransaction transaction, Bitmap bitmap);
}
