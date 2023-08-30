using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Common;

public static class AbsolutePathExtensions
{
    /// <summary>
    /// Helper method to calculate the hash of a given file, reporting progress to the given job.
    /// </summary>
    /// <param name="inputStream"></param>
    /// <param name="job"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<Hash> XxHash64Async(this AbsolutePath input, IJob<Size>? job = null,
        CancellationToken token = default)
    {
        await using var inputStream = input.Read();
        if (job == null)
            return await inputStream.HashingCopyAsync(Stream.Null, token, async m => await Task.CompletedTask);
        else
            return await inputStream.HashingCopyAsync(Stream.Null, token, async m => await job.ReportAsync(Size.FromLong(m.Length), token));
    }

}
