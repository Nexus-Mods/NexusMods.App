using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI;

/// <summary>
/// Optimized storage for images.
/// </summary>
[PublicAPI]
public interface IImageStore
{
    ValueTask Store(EntityId id, Bitmap bitmap);

    ValueTask<Bitmap?> Retrieve(EntityId id);
}
