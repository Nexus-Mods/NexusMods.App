using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.Hashing.xxHash64;

// TODO: Paths library doesn't have an actual dependency on RateLimiting; but requires access to job(s) for reporting. We should probably decouple libraries that don't have strict dependencies on other libraries at some point.

/// <summary>
/// Extensions tied to <see cref="AbsolutePath"/>(s).
/// </summary>
public static class AbsolutePathExtensions
{
    /// <summary>
    /// Asynchronously hashes a given file.
    /// </summary>
    /// <param name="path">Path to the file to be hashed.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <param name="job">The job to which the hashing progress will be reported to.</param>
    public static async Task<Hash> XxHash64Async(this AbsolutePath path, CancellationToken? token = null, IJob<Size>? job = null)
    {
        await using var stream = path.Read();
        return await stream.XxHash64Async(token ?? CancellationToken.None, job);
    }
}
