using NexusMods.Abstractions.Activities;
using NexusMods.Hashing.xxHash64;
using static NexusMods.Paths.Size;
using Size = NexusMods.Paths.Size;

namespace NexusMods.Extensions.Hashing;

/// <summary>
/// Extensions for <see cref="Stream"/>.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Helper method to calculate the hash of a given stream while copying it to another stream. This method will
    /// update the IJob.Process property of the job as it progresses.
    /// </summary>
    /// <param name="inputStream"></param>
    /// <param name="outputStream"></param>
    /// <param name="job"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<Hash> HashingCopyAsync(this Stream inputStream, Stream outputStream, IActivitySource<Size> job,
        CancellationToken token)
    {
        return await inputStream.HashingCopyAsync(outputStream, token, async m =>
        {
            job.AddProgress(FromLong(m.Length));
        });
    }

    /// <summary>
    /// Helper method to calculate the hash of a given stream, reporting progress to the given job.
    /// </summary>
    /// <param name="inputStream"></param>
    /// <param name="job"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static async Task<Hash> XxHash64Async(this Stream inputStream, IActivitySource<Size> job,
        CancellationToken token)
    {
        return await inputStream.HashingCopyAsync(Stream.Null, token, async m =>
        {
            job.AddProgress(Size.FromLong(m.Length));
        });
    }

}
