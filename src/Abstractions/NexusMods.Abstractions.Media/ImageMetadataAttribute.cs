using System.Runtime.InteropServices;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.Media;

/// <summary>
/// Binary blob representation of <see cref="ImageMetadata"/>.
/// </summary>
public class ImageMetadataAttribute(string ns, string name) : BlobAttribute<ImageMetadata>(ns, name)
{
    private static readonly int ImageMetadataSize = Marshal.SizeOf<ImageMetadata>();

    /// <inheritdoc/>
    protected override ImageMetadata FromLowLevel(ReadOnlySpan<byte> value, ValueTags tags, AttributeResolver resolver)
    {
        return ImageMetadata.Read(value);
    }

    /// <inheritdoc/>
    protected override void WriteValue<TWriter>(ImageMetadata value, TWriter writer)
    {
        var span = writer.GetSpan(sizeHint: ImageMetadataSize);
        value.Write(span);
        writer.Advance(ImageMetadataSize);
    }
}
