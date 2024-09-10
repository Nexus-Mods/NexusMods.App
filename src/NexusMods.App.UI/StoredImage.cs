using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.App.UI;

public partial class StoredImage : IModelDefinition
{
    private const string Namespace = "NexusMods.ImageStore.StoredImage";

    public static readonly BitmapDataAttribute BitmapData = new(Namespace, nameof(BitmapData));

    public static readonly ImageMetadataAttribute Metadata = new(Namespace, nameof(Metadata));
}
