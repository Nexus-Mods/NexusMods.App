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
    /// Calculates the xxHash64 of a file by memory mapping it.
    /// </summary>
    /// <param name="input">The path to the file.</param>
    /// <returns>The xxHash64 hash of the file.</returns>
    public static unsafe Hash XxHash64MemoryMapped(this AbsolutePath input)
    {
        using var memoryMappedFile = MemoryMappedFile.CreateFromFile(input.GetFullPath(), FileMode.Open);
        using var accessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        var ptrData = (byte*)accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
        var hashValue = XxHash64Algorithm.HashBytes(new ReadOnlySpan<byte>(ptrData, (int)accessor.Capacity));
        return Hash.From(hashValue);
    }

}
