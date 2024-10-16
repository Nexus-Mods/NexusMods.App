using System.IO.MemoryMappedFiles;
using NexusMods.Hashing.xxHash64;
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

    /// <summary>
    /// Calculates the xxHash64 of a file by memory mapping it.
    /// </summary>
    public static Hash XxHash64MemoryMapped(this AbsolutePath input)
    {
        try
        {
            using var mmf = input.FileSystem.CreateMemoryMappedFile(input, FileMode.Open, MemoryMappedFileAccess.Read, 0);
            var hashValue = XxHash64Algorithm.HashBytes(mmf.AsSpan());
            return Hash.From(hashValue);
        }
        catch (ArgumentException)
        {
            return Hash.From(XxHash64Algorithm.HashOfEmptyFile);
        }
    }
}
