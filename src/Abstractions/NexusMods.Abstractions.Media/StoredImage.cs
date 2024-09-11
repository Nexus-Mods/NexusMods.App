using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Media;

/// <summary>
/// Represent an image.
/// </summary>
[UsedImplicitly]
public partial class StoredImage : IModelDefinition
{
    private const string Namespace = "NexusMods.ImageStore.StoredImage";

    /// <summary>
    /// Image data.
    /// </summary>
    public static readonly ImageDataAttribute ImageData = new(Namespace, nameof(ImageData));

    /// <summary>
    /// Image metadata.
    /// </summary>
    public static readonly ImageMetadataAttribute Metadata = new(Namespace, nameof(Metadata));
}
