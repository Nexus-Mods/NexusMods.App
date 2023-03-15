using System.Buffers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.Hashing.xxHash64;

/// <summary>
/// Hashing related extensions tied to <see cref="Stream"/>(s).
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Calculate the hash of a given stream starting at the current location of the stream
    /// </summary>
    /// <param name="stream">Source Stream</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <param name="job">The job to which the hashing progress will be reported to.</param>
    public static async Task<Hash> XxHash64Async(this Stream stream, CancellationToken token = default, IJob<Size>? job = null)
    {
        return await stream.HashingCopyAsync(Stream.Null, token, job);
    }

    /// <summary>
    /// Perform a stream copy, calculating the xxHash64 in the process
    /// </summary>
    /// <param name="inputStream">Where the input data will be read from.</param>
    /// <param name="outputStream">Where the output data will be placed.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <param name="job">The job to which the hashing progress will be reported to.</param>
    /// <returns></returns>
    public static async Task<Hash> HashingCopyAsync(this Stream inputStream, Stream outputStream,
        CancellationToken token, IJob<Size>? job = null)
    {
        using var rented = MemoryPool<byte>.Shared.Rent(1024 * 1024);
        var buffer = rented.Memory;

        var hasher = new XxHash64Algorithm(0);

        var running = true;
        ulong finalHash = 0;
        while (running && !token.IsCancellationRequested)
        {
            var totalRead = 0;

            while (totalRead != buffer.Length)
            {
                var read = await inputStream.ReadAsync(buffer.Slice(totalRead, buffer.Length - totalRead),
                    token);

                if (read == 0)
                {
                    running = false;
                    break;
                }

                if (job != null)
                    await job.ReportAsync(Size.FromLong(read), token);

                totalRead += read;
            }

            // Note: We can't deduplicate this code [without boxing] because ref param + async.
            // This code is based on the code inside XxHash64Algorithm
            var pendingWrite = outputStream.WriteAsync(buffer[..totalRead], token);
            if (running)
            {
                hasher.TransformByteGroupsInternal(buffer.Span);
                await pendingWrite;
            }
            else
            {
                var preSize = (totalRead >> 5) << 5;
                if (preSize > 0)
                {
                    hasher.TransformByteGroupsInternal(buffer[..preSize].Span);
                    finalHash = hasher.FinalizeHashValueInternal(buffer[preSize..totalRead].Span);
                    await pendingWrite;
                    break;
                }

                finalHash = hasher.FinalizeHashValueInternal(buffer[..totalRead].Span);
                await pendingWrite;
                break;
            }
        }

        await outputStream.FlushAsync(token);
        return Hash.From(finalHash);
    }

    /// <summary>
    /// Perform a stream copy, calculating the xxHash64 inline with the copy routines. For each chunk
    /// of data read, call `fn` with a buffer of the data currently being processed.
    /// </summary>
    /// <param name="inputStream">The source stream</param>
    /// <param name="fn">Function to call with each chunk of data processed</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns>The final cash.</returns>
    public static async Task<Hash> HashingCopyWithFnAsync(this Stream inputStream, Func<Memory<byte>, Task> fn,
        CancellationToken token = default)
    {
        using var rented = MemoryPool<byte>.Shared.Rent(1024 * 1024);
        var buffer = rented.Memory;

        var hasher = new XxHash64Algorithm(0);

        var running = true;
        ulong finalHash = 0;
        while (running && !token.IsCancellationRequested)
        {
            var totalRead = 0;

            while (totalRead != buffer.Length)
            {
                var read = await inputStream.ReadAsync(buffer.Slice(totalRead, buffer.Length - totalRead),
                    token);

                if (read == 0)
                {
                    running = false;
                    break;
                }
                totalRead += read;
            }

            var pendingWrite = fn(buffer[..totalRead]);
            if (running)
            {
                hasher.TransformByteGroupsInternal(buffer.Span);
                await pendingWrite;
            }
            else
            {
                var preSize = (totalRead >> 5) << 5;
                if (preSize > 0)
                {
                    hasher.TransformByteGroupsInternal(buffer[..preSize].Span);
                    finalHash = hasher.FinalizeHashValueInternal(buffer[preSize..totalRead].Span);
                    await pendingWrite;
                    break;
                }

                finalHash = hasher.FinalizeHashValueInternal(buffer[..totalRead].Span);
                await pendingWrite;
                break;
            }
        }

        return Hash.From(finalHash);
    }
}
