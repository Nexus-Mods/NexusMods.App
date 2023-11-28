using System.Buffers;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Common;
using NexusMods.DataModel.Activities;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader.DTOs;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader
{
    /// <summary>
    /// HTTP downloader with the following features<br/>
    ///  - pause/resume support<br/>
    ///  - support for downloading in multiple parallel chunks, effective download speed should roughly be the fastest
    ///    any of the available sources supports<br/>
    ///  - automatically switch to alternative source in case of server error<br/>
    /// </summary>
    ///
    /// <remarks>
    /// This code works by first doing a HEAD request on the sources passed in, this is used to determine the size
    /// of the download. If HEAD isn't supported or no ContentLength is defined, then the code falls back onto a
    /// non-resumable downloader.
    ///
    /// In the case of resumable server support, the download is divided into ChunkSize chunks and each is downloaded
    /// separately. Data is read from the network stream in chunks of `ReadChunk` size. These come from a memory pool
    /// so they don't result in many allocations, but each read block will result in a write and flush to disk and the
    /// saving to disk, which could result in slowdown with slow HDDs if this buffer is too slow.
    /// </remarks>
    public class AdvancedHttpDownloader : IHttpDownloader
    {
        private readonly ILogger<AdvancedHttpDownloader> _logger;
        private readonly HttpClient _client;
        private readonly IActivityFactory _activityFactory;

        // These parameters don't need any kind of user tuning; thus are left non-configurable.
        /// <summary>
        /// The size of each chunk to download, we will never download more than this amount from a single source
        /// before trying other sources and/or threads
        /// </summary>
        private readonly Size _chunkSize = Size.MB * 128;

        private const int MaxRetries = 16;

        /// <summary>
        /// The size of the buffer used to read from the network stream, no need to make this too large as
        /// they are pretty quickly written to disk and returned to the pool. Really these should be fairly
        /// close to the size of the TCP buffers.
        /// </summary>
        private readonly Size _readBlockSize = Size.MB;


        // see IHttpDownloaderSettings for docs about these
        private readonly int _writeQueueLength;
        private readonly int _minCancelAge;
        private readonly double _cancelSpeedFraction;

        // the shared pool is capped at 1MB (2^20 bytes) per buffer so if larger blocks are desired, use a custom pool
        private readonly MemoryPool<byte> _memoryPool;

        /// <summary>
        /// Constructor
        /// </summary>
        public AdvancedHttpDownloader(ILogger<AdvancedHttpDownloader> logger, HttpClient client, IActivityFactory activityFactory,
            IHttpDownloaderSettings settings)
        {
            _logger = logger;
            _client = client;
            _activityFactory = activityFactory;
            _writeQueueLength = settings.WriteQueueLength;
            _minCancelAge = settings.MinCancelAge;
            _cancelSpeedFraction = settings.CancelSpeedFraction;
            _memoryPool = MemoryPool<byte>.Shared;
        }

        /// <inheritdoc />
        public async Task<Hash> DownloadAsync(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, HttpDownloaderState? downloaderState, Size? size, CancellationToken cancel)
        {
            downloaderState ??= new HttpDownloaderState();
            using var primaryJob = _activityFactory.Create<Size>(IHttpDownloader.Group, "Initiating Download {FileName}", destination);

            var features = await ServerFeatures.Request(_client, sources[0].Copy(), cancel);
            size ??= features.DownloadSize;

            // Can't do multipart downloads if we don't know the full size or the server doesn't support
            if (!features.SupportsResume || size == null)
            {
                return await DownloadWithoutResume(sources, destination, downloaderState, size, cancel);
            }

            // Note: All data eventually is piped into primary job (when writing to destination), so we can just use that to track everything as a whole.
            downloaderState.Activity = primaryJob;

            var state = await InitiateState(destination, size.Value, sources, cancel);
            state.Sources = sources.Select((source, idx) => new Source { Request = source, Priority = idx }).ToArray();
            state.Destination = destination;

            var writeQueue = Channel.CreateBounded<WriteOrder>(_writeQueueLength);

            var fileWriter = FileWriterTask(state, writeQueue.Reader, primaryJob, cancel);

            await DownloaderTask(primaryJob, state, writeQueue.Writer, cancel);


            try
            {
                // once the download driver is done (for whatever reason), signal the file writer to finish the rest of the queue and then also end
                writeQueue.Writer.TryComplete();

                await fileWriter;
            }
            catch (OperationCanceledException)
            {
                // ignore
                return Hash.Zero;
            }

            return await FinalizeDownload(state, cancel);
        }

        private async Task<Hash> DownloadWithoutResume(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination,
            HttpDownloaderState downloaderState, Size? size, CancellationToken cancel)
        {
            using var primaryJob = _activityFactory.Create<Size>(IHttpDownloader.Group, "Downloading {FileName}", destination);

            using var buffer = _memoryPool.Rent((int)_readBlockSize.Value);
            foreach (var source in sources)
            {
                var response = await _client.SendAsync(source.Copy(), cancel);
                if (!response.IsSuccessStatusCode) continue;

                await using var of = destination.Create();
                await using var stream = await response.Content.ReadAsStreamAsync(cancel);
                return await stream.HashingCopyAsync(of, primaryJob, cancel);
            }

            throw new Exception("No valid server");
        }

        private static async Task<Hash> FinalizeDownload(DownloadState state, CancellationToken cancel)
        {
            var tempPath = state.TempFilePath;

            if (state.HasIncompleteChunk) return Hash.Zero;

            state.StateFilePath.Delete();
            File.Move(tempPath.ToString(), state.Destination.ToString(), true);
            return await state.Destination.XxHash64Async(token: cancel);
        }

        #region Output Writing
        private async Task FileWriterTask(DownloadState state, ChannelReader<WriteOrder> writes, IActivitySource<Size> job, CancellationToken cancel)
        {
            var tempPath = state.TempFilePath;
            await using var file = tempPath.Open(FileMode.OpenOrCreate, FileAccess.Write);
            file.SetLength((long)(ulong)state.TotalSize);

            while (true)
            {
                // writing out data that was already downloaded is intentionally not cancellable. The writes channel
                // will be closed on cancellation so this loop will definitively end once all remaining blocks have
                // been written
                try
                {
                    var order = await writes.ReadAsync(cancel);
                    file.Position = (long)order.Offset.Value;
                    await file.WriteAsync(order.Data, cancel);
                    await file.FlushAsync(cancel);
                    order.Owner.Dispose();

                    order.Chunk.Completed += Size.From((ulong)order.Data.Length);

                    await WriteDownloadState(state);
                    // _bufferPool.Return(order.Data);
                    await job.AddProgress(Size.FromLong(order.Data.Length), cancel);
                }
                catch (ChannelClosedException)
                {
                    break;
                }
            }
        }
        #endregion

        #region Downloading

        private async Task DownloaderTask(IActivitySource<Size> activitySource, DownloadState state, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            var finishReward = 1024;

            // start one task per unfinished chunk. We never start additional tasks but a task may take on another chunks
            await Task.WhenAll(state.UnfinishedChunks.Select(async chunk =>
            {
                try
                {
                    while (chunk.Read < chunk.Size)
                    {
                        await DownloadChunk(activitySource, state, chunk, writes, MakeChunkCancelToken(chunk, cancel));

                        // boost source priority on completion, first one gets the biggest boost
                        chunk.Source!.Priority -= finishReward;
                        finishReward /= 2;

                        chunk = PickNextChunk(state, chunk);
                    }
                }
                catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
                {
                    // ignore cancellation
                }
            }).ToArray());
        }

        private ChunkState PickNextChunk(DownloadState state, ChunkState chunk)
        {
            var slowChunk = FindSlowChunk(state.UnfinishedChunks, chunk.Source!, chunk.BytesPerSecond);
            if (slowChunk == null) return chunk;

            chunk = StealWork(slowChunk);
            lock (state)
            {
                state.Chunks.Add(chunk);
            }

            return chunk;
        }

        private bool IsDownloadSlow(ChunkState chunk, Bandwidth referenceSpeed)
        {
            var now = DateTime.Now;
            if ((now - chunk.Started).TotalMilliseconds < _minCancelAge)
            {
                // don't cancel downloads that were only just started
                return false;
            }
            if (chunk.Read / chunk.Size > 0.8f)
            {
                // don't cancel downloads that are almost done
                return false;
            }

            return chunk.BytesPerSecond.Value < referenceSpeed.Value * _cancelSpeedFraction;
        }

        private ChunkState? FindSlowChunk(IEnumerable<ChunkState> chunks, Source source, Bandwidth speed)
        {
            return chunks.Aggregate<ChunkState, ChunkState?>(null, (prev, iter) =>
            {
                if (iter.Source != source
                    && iter.Cancel?.IsCancellationRequested == false
                    && IsDownloadSlow(iter, speed)
                    && (prev == null || iter.BytesPerSecond.Value < prev.BytesPerSecond.Value))
                {
                    return iter;
                }

                return prev;
            });
        }

        private static ChunkState StealWork(ChunkState slowChunk)
        {
            slowChunk.Cancel?.Cancel();

            var newChunk = new ChunkState
            {
                Offset = slowChunk.Offset + slowChunk.Read,
                Size = slowChunk.Size - slowChunk.Read
            };
            lock (slowChunk.Source!)
            {
                {
                    slowChunk.Source.Priority += 1000;
                    slowChunk.Size = slowChunk.Read;
                }
            }

            return newChunk;
        }

        private static CancellationToken MakeChunkCancelToken(ChunkState chunk, CancellationToken cancel)
        {
            chunk.Cancel = new CancellationTokenSource();
            return CancellationTokenSource.CreateLinkedTokenSource(new[] { chunk.Cancel.Token, cancel }).Token;
        }

        private async Task<HttpResponseMessage?> SendRangeRequest(DownloadState state, ChunkState chunk, CancellationToken cancel)
        {
            // Caller passes a full http request but we need to adjust the headers so need a copy
            var request = chunk.Source!.Request!.Copy();

            var from = chunk.Offset + chunk.Read;
            var to = chunk.Offset + chunk.Size - Size.One;

            Debug.Assert(to < state.TotalSize);
            request.Headers.Range = new RangeHeaderValue((long)from.Value, (long)to.Value);

            HttpResponseMessage response;
            var retries = 0;

            while(true)
            {
                try
                {
                    response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);
                    break;

                }
                catch (HttpRequestException)
                {
                    if (retries >= MaxRetries) throw;
                    retries += 1;
                    request = request.Copy();
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                // TODO should be retrying the source once at least
                _logger.LogWarning("Failed to download {Source}: {StatusCode}", request.RequestUri, response.StatusCode);
                return null;
            }

            return response;
        }

        private async Task DownloadChunk(IActivitySource<Size> activitySource, DownloadState download, ChunkState chunk, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            HttpResponseMessage? response = null;

            var retries = 0;
            while (!chunk.IsReadComplete)
            {
                var sourceValid = false;
                while (!sourceValid)
                {
                    // this is not an endless loop, TakeSource will throw an exception if all sources were tried and rejected
                    chunk.Source = TakeSource(download);

                    response = await SendRangeRequest(download, chunk, cancel);
                    sourceValid = response != null;
                    if (!sourceValid)
                    {
                        chunk.Source.Priority = int.MaxValue;
                    }
                }

                if (response == null) continue;

                try
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancel);
                    await ReadStreamToEnd(activitySource, stream, chunk, writes, cancel);
                }
                catch (SocketException)
                {
                    if (retries > MaxRetries) throw;
                    retries += 1;
                }
            }
        }

        private static Source TakeSource(DownloadState download)
        {
            lock (download)
            {
                var res = download.Sources?.Aggregate((prev, iter) => iter.Priority < prev.Priority ? iter : prev);
                if (res == null || res.Priority == int.MaxValue)
                {
                    throw new InvalidOperationException("no valid download sources");
                }
                res.Priority += download.Sources?.Length ?? 0;
                return res;
            }
        }

        private async Task ReadStreamToEnd(IActivitySource<Size> job, Stream stream, ChunkState chunk, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            var offset = chunk.Offset + chunk.Read;
            var upperBounds = chunk.Offset + chunk.Size;
            chunk.Started = DateTime.Now;
            while (offset < upperBounds)
            {
                var rented = _memoryPool.Rent((int)_readBlockSize.Value);
                var filledBuffer = await FillBuffer(stream, rented.Memory, chunk.RemainingToRead, cancel);

                var lastRead = Size.FromLong(filledBuffer.Length);
                if (lastRead == Size.Zero) break;

                await job.AddProgress(lastRead, cancel);
                if (lastRead <= Size.Zero) continue;
                chunk.Read += lastRead;

                await writes.WriteAsync(new WriteOrder
                {
                    Offset = offset,
                    Data = filledBuffer,
                    Owner = rented,
                    Chunk = chunk
                }, cancel);
                offset += lastRead;
            }
        }

        /// <summary>
        /// Tries to fill the buffer with data from the stream. Returns a view of the actual buffer once it's full or the stream
        /// has ended.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        /// <param name="totalSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static async ValueTask<Memory<byte>> FillBuffer(Stream stream, Memory<byte> data, Size totalSize, CancellationToken token)
        {
            var totalRead = 0;
            while (totalRead < data.Length && totalRead < (long)totalSize.Value)
            {
                var read = await stream.ReadAsync(data[totalRead..], token);
                if (read == 0) break;
                totalRead += read;
            }
            return data[..totalRead];
        }

        #endregion // Downloading

        #region Download State Persistance

        private async Task<DownloadState> InitiateState(AbsolutePath destination, Size size, IReadOnlyList<HttpRequestMessage> sourceMessages, CancellationToken cancel)
        {

            DownloadState? state = null;
            var stateFilePath = DownloadState.GetStateFilePath(destination);
            if (stateFilePath.FileExists && DownloadState.GetTempFilePath(destination).FileExists)
            {
                _logger.LogInformation("Resuming prior download {FilePath}", destination);
                state = await DeserializeDownloadState(stateFilePath, cancel);
                state.ResumeCount += 1;
            }

            var sources = sourceMessages.Select(msg =>
                new Source{
                    Request = msg,
                    Priority = 0,
                }).ToArray();

            if (state != null)
            {
                // normalize input chunks so we don't fail to resume if the previous download was interrupted at a bad time
                state.Chunks = state.Chunks.Select(chunk => new ChunkState
                {
                    Completed = chunk.Completed,
                    Offset = chunk.Offset,
                    Read = chunk.Completed,
                    Size = chunk.Size,
                }).ToList();
                state.Sources = sources;

            }
            else
            {
                List<ChunkState> chunks = new();
                for (var offset = Size.Zero; offset < size; offset += _chunkSize)
                {
                    chunks.Add(new ChunkState
                    {
                        Offset = offset,
                        Size = Size.From(Math.Min(_chunkSize.Value, size.Value - offset.Value)),
                    });
                }

                _logger.LogInformation("Starting download {FilePath} in {ChunkCount} chunks of {Size} ", destination, chunks.Count, _chunkSize);

                state = new DownloadState
                {
                    TotalSize = size,
                    Sources = sources.ToArray(),
                    Destination = destination,
                    Chunks = chunks
                };
            }

            return state;
        }

        private async Task WriteDownloadState(DownloadState state)
        {
            await using var fs = state.StateFilePath.Create();
            try
            {
                await JsonSerializer.SerializeAsync(fs, state, cancellationToken: CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to write download state");
            }
        }

        private static async ValueTask<DownloadState> DeserializeDownloadState(AbsolutePath path, CancellationToken token)
        {
            await using var fs = path.Read();
            var res = await JsonSerializer.DeserializeAsync<DownloadState>(fs, JsonSerializerOptions.Default, token);

            return res ?? new DownloadState();
        }

        #endregion // Download State Persistence

    }
}
