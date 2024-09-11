namespace NexusMods.Abstractions.Media;

/// <summary>
/// Image data.
/// </summary>
public readonly struct ImageData
{
    /// <summary>
    /// Compression type.
    /// </summary>
    public readonly ImageDataCompression Compression;

    /// <summary>
    /// Binary data.
    /// </summary>
    public readonly byte[] Data;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ImageData(ImageDataCompression compression, byte[] data)
    {
        Compression = compression;
        Data = data;
    }
}

/// <summary>
/// Compression types.
/// </summary>
public enum ImageDataCompression : byte
{
    /// <summary>
    /// No compression.
    /// </summary>
    None = 0,
}
