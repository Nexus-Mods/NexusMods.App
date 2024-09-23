using System.Diagnostics;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.Abstractions.Media;

/// <summary>
/// Binary blob containing image data.
/// </summary>
public class ImageDataAttribute(string ns, string name) : BlobAttribute<ImageData>(ns, name)
{
    /// <inheritdoc/>
    protected override ImageData FromLowLevel(ReadOnlySpan<byte> value, ValueTags tags, AttributeResolver resolver)
    {
        Debug.Assert(sizeof(ImageDataCompression) == 1);
        var compression = (ImageDataCompression)value[0];

        var data = Decompress(compression, value[1..]);
        return new ImageData(compression, data);
    }

    /// <inheritdoc/>
    protected override void WriteValue<TWriter>(ImageData value, TWriter writer)
    {
        Debug.Assert(sizeof(ImageDataCompression) == 1);
        var count = value.Data.Length + sizeof(ImageDataCompression);

        var span = writer.GetSpan(sizeHint: count);
        span[0] = (byte)value.Compression;

        var bytesWritten = Compress(value.Compression, value.Data, span[1..]);
        writer.Advance(bytesWritten + sizeof(ImageDataCompression));
    }

    private static int Compress(ImageDataCompression compression, byte[] data, Span<byte> outputSpan)
    {
        switch (compression)
        {
            case ImageDataCompression.None:
                data.CopyTo(outputSpan);
                return data.Length;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static byte[] Decompress(ImageDataCompression compression, ReadOnlySpan<byte> data)
    {
        switch (compression)
        {
            case ImageDataCompression.None:
                return data.ToArray();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
