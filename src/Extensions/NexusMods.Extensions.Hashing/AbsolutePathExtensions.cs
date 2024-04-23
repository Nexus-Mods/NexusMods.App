using System.IO.MemoryMappedFiles;
using NexusMods.Abstractions.Activities;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Size = NexusMods.Paths.Size;

namespace NexusMods.Extensions.Hashing;

/// <summary>
/// Extensions for <see cref="AbsolutePath"/>.
/// </summary>
public static class AbsolutePathExtensions
{
    /// <summary>
    /// Helper method to calculate the hash of a given file, reporting progress to the given job.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="job"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<Hash> XxHash64Async(this AbsolutePath input, IActivitySource<Size>? job = null,
        CancellationToken token = default)
    {
        await using var inputStream = input.Read();
        if (job == null)
            return await inputStream.HashingCopyAsync(Stream.Null, token, async m => await Task.CompletedTask);
        else
            return await inputStream.HashingCopyAsync(Stream.Null, token,  async m =>
            {
                job.AddProgress(Size.FromLong(m.Length));
            });
    }

    /// <summary>
    /// Calculates the xxHash64 of a file by memory mapping it and reports progress upon completion.
    /// </summary>
    /// <param name="input">The path to the file.</param>
    /// <param name="job">The job to report progress to.</param>
    /// <returns>The xxHash64 hash of the file.</returns>
    public static Hash XxHash64MemoryMapped(this AbsolutePath input, IActivitySource<Size>? job = null)
    {
        try
        {
            using var mmf = input.FileSystem.CreateMemoryMappedFile(input, FileMode.Open, MemoryMappedFileAccess.Read);
            var hashValue = XxHash64Algorithm.HashBytes(mmf.AsSpan());
            job?.AddProgress(Size.FromLong((long)mmf.Length));
            return Hash.From(hashValue);
        }
        catch (ArgumentException)
        {
            return Hash.From(XxHash64Algorithm.HashOfEmptyFile);
        }
    }
}
