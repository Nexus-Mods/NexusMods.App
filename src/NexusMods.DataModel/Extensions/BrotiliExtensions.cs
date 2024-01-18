using System.IO.Compression;
using System.Text.Json;

namespace NexusMods.DataModel.Extensions;

/// <summary>
/// In general, Brotli compression is better than GZip compression for Json files and the performance is
/// close enough to other compression methods for small files that it's useful for embedded resources and
/// blob storage.
/// </summary>
public static class BrotiliExtensions
{
    /// <summary>
    /// Reads a Brotili compressed json file into an object.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async ValueTask<T?> ReadBrotiliJson<T>(this Stream stream, JsonSerializerOptions options)
    {
        await using var brotliStream = new BrotliStream(stream, CompressionMode.Decompress);
        return await JsonSerializer.DeserializeAsync<T>(brotliStream, options);
    }

    /// <summary>
    /// Writes an object to a stream as a Brotili compressed json file.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    /// <param name="level"></param>
    /// <typeparam name="T"></typeparam>
    public static async ValueTask WriteAsBrotiliJson<T>(this Stream stream, T value, JsonSerializerOptions options, CompressionLevel level = CompressionLevel.SmallestSize)
    {
        await using var brotliStream = new BrotliStream(stream, level);
        await JsonSerializer.SerializeAsync(brotliStream, value, options);
    }
}
