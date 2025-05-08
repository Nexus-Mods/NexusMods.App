using System.IO.Hashing;
using System.IO.MemoryMappedFiles;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Extensions.Hashing;

/// <summary>
/// Extensions for <see cref="AbsolutePath"/>.
/// </summary>
public static class AbsolutePathExtensions
{
    /// <summary>
    /// Helper method to calculate the hash of a given file.
    /// </summary>
    public static async Task<Hash> XxHash64Async(this AbsolutePath input, CancellationToken token = default)
    {
        await using var inputStream = input.Read();
        return await inputStream.HashingCopyAsync(Stream.Null, token, static _ => Task.CompletedTask);
    }

    private static readonly Hash HashOfEmptyFile = Hash.From(XxHash3.HashToUInt64(ReadOnlySpan<byte>.Empty));

    /// <summary>
    /// Calculates the xxHash64 of a file by memory mapping it.
    /// </summary>
    public static Hash XxHash64MemoryMapped(this AbsolutePath input)
    {
        try
        {
            using var mmf = input.FileSystem.CreateMemoryMappedFile(input, FileMode.Open, MemoryMappedFileAccess.Read, 0);
            var hashValue = XxHash3.HashToUInt64(mmf.AsSpan());
            return Hash.From(hashValue);
        }
        catch (ArgumentException)
        {
            return HashOfEmptyFile;
        }
    }
}
