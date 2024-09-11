using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace NexusMods.Abstractions.Media;

/// <summary>
/// Metadata of an image.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct ImageMetadata
{
    /// <summary>
    /// Width.
    /// </summary>
    public readonly uint ImageWidth;

    /// <summary>
    /// Height.
    /// </summary>
    public readonly uint ImageHeight;

    /// <summary>
    /// Color type.
    /// </summary>
    public readonly SKColorType SkColorType;

    /// <summary>
    /// Alpha format.
    /// </summary>
    public readonly AlphaFormat AlphaFormat;

    /// <summary>
    /// DPI.
    /// </summary>
    public readonly uint Dpi;

    /// <summary>
    /// Pixel size.
    /// </summary>
    public PixelSize PixelSize => new((int)ImageWidth, (int)ImageHeight);

    /// <summary>
    /// Pixel format.
    /// </summary>
    public PixelFormat PixelFormat => SkColorType.ToPixelFormat();

    // NOTE(erri120): Going from bits to bytes requires dividing by 8, aka bit shift by 3

    /// <summary>
    /// Stride is the number of bytes from one row pixels in memory to the next row.
    /// </summary>
    public int Stride => ((int)ImageWidth * PixelFormat.BitsPerPixel) >> 3;

    /// <summary>
    /// Total length of the raw data.
    /// </summary>
    public ulong DataLength => (ImageWidth * ImageHeight * (uint)PixelFormat.BitsPerPixel) >> 3;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ImageMetadata(uint imageWidth, uint imageHeight, SKColorType skColorType, AlphaFormat alphaFormat, uint dpi)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        SkColorType = skColorType;
        AlphaFormat = alphaFormat;
        Dpi = dpi;
    }

    /// <summary>
    /// Reads the binary data as metadata.
    /// </summary>
    public static ImageMetadata Read(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == Marshal.SizeOf<ImageMetadata>());

        unsafe
        {
            fixed (byte* b = bytes)
            {
                return Unsafe.Read<ImageMetadata>(b);
            }
        }
    }

    /// <summary>
    /// Writes the metadata as binary data.
    /// </summary>
    public void Write(Span<byte> bytes)
    {
        Debug.Assert(bytes.Length == Marshal.SizeOf<ImageMetadata>());

        unsafe
        {
            fixed (void* b = bytes)
            {
                Unsafe.Write(b, this);
            }
        }
    }
}
