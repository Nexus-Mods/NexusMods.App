using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Networking.HttpDownloader;

internal static class LocalFileDownloader
{
    public static async Task<Hash?> TryDownloadLocal(IJob<IHttpDownloader, Size> job, IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination)
    {
        foreach (var source in sources)
        {
            var result = await TryDownloadLocal(job, source, destination);
            if (result != null)
                return result.Value;
        }

        return null;
    }
    
    public static async Task<Hash?> TryDownloadLocal(IJob<IHttpDownloader, Size> job, HttpRequestMessage source, AbsolutePath destination)
    {
        var uri = source.RequestUri!;
        if (!uri.IsFile)
            return null;

        // Report finished.
        var filePath = Uri.UnescapeDataString(uri.AbsolutePath);
        var fullPath = Path.GetFullPath(filePath);

        await using var stream = fullPath.ToAbsolutePath(FileSystem.Shared).Read();
        job.Size = Size.FromLong(stream.Length);
        await using var file = destination.Create();
        return await stream.HashingCopyAsync(file, default, job);
    }
}
