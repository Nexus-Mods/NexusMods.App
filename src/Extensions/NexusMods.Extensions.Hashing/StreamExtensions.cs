using NexusMods.Hashing.xxHash3;

namespace NexusMods.Extensions.Hashing;

/// <summary>
/// Extensions for <see cref="Stream"/>.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Helper method to calculate the hash of a given stream while copying it to another stream.
    /// </summary>
    public static async Task<Hash> HashingCopyAsync(this Stream inputStream, Stream outputStream, CancellationToken token)
    {
        return await inputStream.HashingCopyAsync(outputStream, token, static _ => Task.CompletedTask);
    }

    /// <summary>
    /// Helper method to calculate the hash of a given stream.
    /// </summary>
    public static async Task<Hash> XxHash3Async(this Stream inputStream, CancellationToken token)
    {
        return await inputStream.HashingCopyAsync(Stream.Null, token, static _ => Task.CompletedTask);
    }

}
