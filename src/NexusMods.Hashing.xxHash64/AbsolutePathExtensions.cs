using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.Hashing.xxHash64;

public static class AbsolutePathExtensions
{
    public static async Task<Hash> XxHash64(this AbsolutePath path, CancellationToken? token = null, IJob<Size>? job = null)
    {
        await using var stream = path.Read();
        return await stream.Hash(token ?? CancellationToken.None, job);
    }
    
}