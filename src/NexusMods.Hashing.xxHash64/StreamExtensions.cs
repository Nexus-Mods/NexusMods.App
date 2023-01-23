using System.Buffers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Paths;

namespace NexusMods.Hashing.xxHash64;

public static class StreamExtensions
{
    /// <summary>
    /// Calculate the hash of a given stream starting at the current location of the stream
    /// </summary>
    /// <param name="stream">Source Stream</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public static async Task<Hash> Hash(this Stream stream, CancellationToken token = default, IJob<Size>? job = null)
    {
        return await stream.HashingCopy(Stream.Null, token, job);
    }

    
    /// <summary>
    /// Perform a stream copy, calculating the hash in the process
    /// </summary>
    /// <param name="inputStream"></param>
    /// <param name="outputStream"></param>
    /// <param name="token"></param>
    /// <param name="job"></param>
    /// <returns></returns>
    public static async Task<Hash> HashingCopy(this Stream inputStream, Stream outputStream,
        CancellationToken token, IJob<Size>? job = null)
    {
        using var rented = MemoryPool<byte>.Shared.Rent(1024 * 1024);
        var buffer = rented.Memory;

        var hasher = new xxHashAlgorithm(0);

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
                    await job.Report(read, token);

                totalRead += read;
            }

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

        return xxHash64.Hash.From(finalHash);
    }
    
    /// <summary>
    /// Perform a stream copy, calculating the hash inline with the copy routines. For each chunk
    /// of data read, call `fn` with a buffer of the data currently being processed.
    /// </summary>
    /// <param name="inputStream">The source stream</param>
    /// <param name="fn">Function to call with each chunk of data processed</param>
    /// <param name="token">Cancellation Token</param>
    /// <returns></returns>
    public static async Task<Hash> HashingCopyWithFn(this Stream inputStream, Func<Memory<byte>, Task> fn,
        CancellationToken token = default)
    {
        using var rented = MemoryPool<byte>.Shared.Rent(1024 * 1024);
        var buffer = rented.Memory;

        var hasher = new xxHashAlgorithm(0);

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
        
        return xxHash64.Hash.From(finalHash);
    }
}