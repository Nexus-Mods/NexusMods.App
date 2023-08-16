using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Common;

public static class StreamExtensions
{
    /// <summary>
    /// Helper method to calculate the hash of a given stream while copying it to another stream. This method will
    /// update the <see cref="IJob{TSize}.Progress"/> property of the job as it progresses.
    /// </summary>
    /// <param name="inputStream"></param>
    /// <param name="outputStream"></param>
    /// <param name="job"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<Hash> HashingCopyAsync(this Stream inputStream, Stream outputStream, IJob<Size> job,
        CancellationToken token)
    {
        return await inputStream.HashingCopyAsync(outputStream, token, async m => await job.ReportAsync(Size.FromLong(m.Length), token));
    }

}
