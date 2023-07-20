using System.Buffers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader.DTOs;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader
{
    using DLJob = IJob<IHttpDownloader, Size>;

    /// <summary>
    /// HTTP downloader with the following features<br/>
    ///  - pause/resume support<br/>
    ///  - support for downloading in multiple parallel chunks, effective download speed should roughly be the fastest
    ///    any of the available sources supports<br/>
    ///  - automatically switch to alternative source in case of server error<br/>
    /// </summary>
    ///
    /// <remarks>
    /// Workflow:<br/>
    ///   - A write job is started that opens the output file for writing and waits for write orders from the<br/>
    ///     downloaders<br/>
    ///   - Before the download can be chunked, an initial request to the server has to be made to determine<br/>
    ///     if range requests are allowed and how large the file is. This chunk shouldn't be too small, chunking tiny<br/>
    ///     files is pointless overhead<br/>
    ///   - Once this initial response is received, the remaining file size, if any, is broken up into a fixed number
    ///     of chunk<br/>
    ///   - worker jobs are generated to handle each chunk<br/>
    ///   - worker jobs follow a work stealing strategy, so when a worker job is finished it will look at the slowest other
    ///     job (the one with the least amount of progress). Unless that job has only just started or is also close to being
    ///     done, that job is canceled<br/>
    ///   - if a job is canceled by a faster job, the faster job deprioritizes the slow source and restarts itself with a new
    ///     chunk for the remaining range of the canceled one.<br/>
    ///   - a canceled job will still submit its last already-downloaded block if necessary before ending.<br/>
    ///   - the hash is generated once the whole file is downloaded<br/>
    ///
    /// TODO:<br/>
    ///   - make parameters configurable<br/>
    ///   - bandwidth throttling<br/>
    ///   - currently only checks the first server for range request support, if it doesn't have it we just continue that<br/>
    ///     download as an all-or-nothing fetch<br/>
    ///   - have a separate watchdog task to cancel slow downloads. Currently, each task that finishes may cancel the slowest
    ///     one and take over. There is a possible scenario where n-1 tasks finish quickly
    ///     before the nth one is eligible to be canceled (MinCancelAge) and that task then takes forever and doesn't get
    ///     canceled because all other ones have already ended.<br/>.
    ///     As a result, MinCancelAge is set quite low which may lead to unnecessary cancellations.<br/>
    /// </remarks>
    public class AdvancedHttpDownloader : IHttpDownloader
    {
        private readonly ILogger<AdvancedHttpDownloader> _logger;
        private readonly HttpClient _client;
        private readonly IResource<IHttpDownloader, Size> _limiter;

        // These parameters don't need any kind of user tuning; thus are left non-configurable.
        /// <summary>
        /// The size of each chunk to download, we will never download more than this amount from a single source
        /// before trying other sources and/or threads
        /// </summary>
        private readonly Size ChunkSize = Size.MB * 128;
        
        /// <summary>
        /// The size of the buffer used to read from the network stream, no need to make this too large as
        /// they are pretty quickly written to disk and returned to the pool. Really these should be fairly
        /// close to the size of the TCP buffers.
        /// </summary>
        private readonly Size ReadBlockSize = Size.MB;


        // see IHttpDownloaderSettings for docs about these
        private readonly int _writeQueueLength;
        private readonly int _minCancelAge;
        private readonly double _cancelSpeedFraction;

        // the shared pool is capped at 1MB (2^20 bytes) per buffer so if larger blocks are desired, use a custom pool
        private readonly MemoryPool<byte> _memoryPool;

        /// <summary>
        /// Constructor
        /// </summary>
        public AdvancedHttpDownloader(ILogger<AdvancedHttpDownloader> logger, HttpClient client, IResource<IHttpDownloader, Size> limiter, 
            IHttpDownloaderSettings settings)
        {
            _logger = logger;
            _client = client;
            _limiter = limiter;
            _writeQueueLength = settings.WriteQueueLength;
            _minCancelAge = settings.MinCancelAge;
            _cancelSpeedFraction = settings.CancelSpeedFraction;
            _memoryPool = MemoryPool<byte>.Shared;
        }

        /// <inheritdoc />
        public async Task<Hash> DownloadAsync(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, HttpDownloaderState? downloaderState, Size? size, CancellationToken cancel)
        {
            downloaderState ??= new HttpDownloaderState();
            using var primaryJob = await _limiter.BeginAsync($"Initiating Download {destination.FileName}", size ?? Size.One, cancel);
            
            var features = await ServerFeatures.Request(_client, sources[0].Copy(), cancel);
            size ??= features.DownloadSize;
            
            // Can't do multipart downloads if we don't know the full size or the server doesn't support
            if (!features.SupportsResume || size == null)
            {
                throw new Exception("Server does not support multipart downloads");
            }

            // Note: All data eventually is piped into primary job (when writing to destination), so we can just use that to track everything as a whole.
            downloaderState.Jobs.Add(primaryJob);
            
            var state = await InitiateState(destination, size.Value, sources, cancel);
            state.Sources = sources.Select((source, idx) => new Source { Request = source, Priority = idx }).ToArray();
            state.Destination = destination;

            var writeQueue = Channel.CreateBounded<WriteOrder>(_writeQueueLength);

            var fileWriter = FileWriterTask(state, writeQueue.Reader, primaryJob, cancel);

            await DownloaderTask(primaryJob, state, writeQueue.Writer, cancel);

            // once the download driver is done (for whatever reason), signal the file writer to finish the rest of the queue and then also end
            writeQueue.Writer.Complete();
            await fileWriter;

            return await FinalizeDownload(state, cancel);
        }

        private async Task<Hash> FinalizeDownload(DownloadState state, CancellationToken cancel)
        {
            var tempPath = state.TempFilePath;

            if (state.HasIncompleteChunk) return Hash.Zero;
            
            state.StateFilePath.Delete();
            File.Move(tempPath.ToString(), state.Destination.ToString(), true);
            return await state.Destination.XxHash64Async(cancel);
        }

        #region Output Writing
        private async Task FileWriterTask(DownloadState state, ChannelReader<WriteOrder> writes, DLJob job, CancellationToken cancel)
        {
            var tempPath = state.TempFilePath;

            var sizeKnown = false;

            await using var file = tempPath.Open(FileMode.OpenOrCreate, FileAccess.Write);
            while (true)
            {
                // writing out data that was already downloaded is intentionally not cancellable. The writes channel
                // will be closed on cancellation so this loop will definitively end once all remaining blocks have
                // been written
                try
                {
                    var order = await writes.ReadAsync(CancellationToken.None);
                    if (!sizeKnown && state.TotalSize > 0)
                    {
                        // technically we don't need this as the FileStream is happy to resize the file as necessary
                        // as we seek around but that's not true for Streams in general plus it feels cleaner to
                        // allocate the space right away
                        file.SetLength(state.TotalSize);
                        sizeKnown = true;
                    }

                    await ApplyWriteOrder(file, order, state);
                    // _bufferPool.Return(order.Data);
                    await job.ReportAsync(Size.FromLong(order.Data.Length), cancel);
                }
                catch (ChannelClosedException _)
                {
                    break;
                }
            }
        }

        private async Task ApplyWriteOrder(Stream file, WriteOrder order, DownloadState state)
        {
            file.Position = order.Offset;
            await file.WriteAsync(order.Data, CancellationToken.None);
            
            UpdateChunk(state, order);

            // data file has to be flushed before updating the state file. If the state is outdated that's ok, on resume
            //   we'll just download a bit more than necessary. But the other way around the output file would be corrupted.
            //   if this is a performance issue, either increase the block size or tie the state update to the automatic file flushing
            //   (something like file.OnFlush(() => WriteDownloadState(state))
            await file.FlushAsync(CancellationToken.None);
            order.Owner.Dispose();

            await WriteDownloadState(state, CancellationToken.None);
        }

        private void UpdateChunk(DownloadState state, WriteOrder order)
        {
            lock (state)
            {
                var chunkIdx = state.Chunks.FindIndex(chunk =>
                    (chunk.Offset <= order.Offset)
                    && ((chunk.Size == -1)
                        || (order.Offset < (chunk.Offset + chunk.Size))));

                state.Chunks[chunkIdx].Completed += order.Data.Length;
            }
        }

        #endregion

        #region Downloading

        private async Task DownloaderTask(DLJob job, DownloadState state, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            var finishReward = 1024;

            // start one task per unfinished chunk. We never start additional tasks but a task may take on another chunks
            await Task.WhenAll(state.UnfinishedChunks.Select(async chunk =>
            {
                using var chunkJob = await _limiter.BeginAsync($"Download Chunk @${chunk.Offset}", Size.FromLong(chunk.Size), cancel);

                try
                {
                    while (chunk.Read < chunk.Size)
                    {
                        await DownloadChunk(chunkJob, state, chunk, writes, MakeChunkCancelToken(chunk, cancel));

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
            if (slowChunk != null)
            {
                _logger.LogInformation("canceling chunk {}-{} @ {}, downloading at {} kb/s",
                    slowChunk.Offset, slowChunk.Offset + slowChunk.Size, slowChunk.Source,
                    slowChunk.KBytesPerSecond);

                chunk = StealWork(slowChunk);
                lock (state)
                {
                    state.Chunks.Add(chunk);
                }
            }

            return chunk;
        }

        private bool IsDownloadSlow(ChunkState chunk, double referenceSpeed)
        {
            var now = DateTime.Now;
            if ((now - chunk.Started).TotalMilliseconds < _minCancelAge)
            {
                // don't cancel downloads that were only just started
                return false;
            }
            if (((float)chunk.Read / chunk.Size) > 0.8f)
            {
                // don't cancel downloads that are almost done
                return false;
            }

            return chunk.BytesPerSecond < referenceSpeed * _cancelSpeedFraction;
        }

        private ChunkState? FindSlowChunk(IEnumerable<ChunkState> chunks, Source source, double speed)
        {
            return chunks.Aggregate<ChunkState, ChunkState?>(null, (prev, iter) =>
            {
                if ((iter.Source != source)
                    && (iter.Cancel?.IsCancellationRequested == false)
                    && IsDownloadSlow(iter, speed)
                    && ((prev == null) || (iter.BytesPerSecond < prev.BytesPerSecond)))
                {
                    return iter;
                }

                return prev;
            });
        }

        private ChunkState StealWork(ChunkState slowChunk)
        {
            slowChunk.Cancel?.Cancel();

            var newChunk = new ChunkState
            {
                Completed = 0,
                Offset = slowChunk.Offset + slowChunk.Read,
                Read = 0,
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

        private CancellationToken MakeChunkCancelToken(ChunkState chunk, CancellationToken cancel)
        {
            chunk.Cancel = new CancellationTokenSource();
            return CancellationTokenSource.CreateLinkedTokenSource(new[] { chunk.Cancel.Token, cancel }).Token;
        }

        private async Task<HttpResponseMessage?> SendRangeRequest(ChunkState chunk, CancellationToken cancel)
        {
            // Caller passes a full http request but we need to adjust the headers so need a copy
            var request = chunk.Source!.Request!.Copy();
            request.Headers.Range = MakeRangeHeader(chunk);

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);

            if (!response.IsSuccessStatusCode)
            {
                // TODO should be retrying the source once at least
                _logger.LogWarning("Failed to download {Source}: {StatusCode}", request.RequestUri, response.StatusCode);
                return null;
            }

            return response;
        }

        private async Task DownloadChunk(DLJob job, DownloadState download, ChunkState chunk, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            var sourceValid = false;

            HttpResponseMessage? response = null;

            while (!sourceValid)
            {
                // this is not an endless loop, TakeSource will throw an exception if all sources were tried and rejected
                chunk.Source = TakeSource(download);

                response = await SendRangeRequest(chunk, cancel);
                sourceValid = response != null;
                if (!sourceValid)
                {
                    chunk.Source.Priority = int.MaxValue;
                }
            }

            if (response != null)
            {
                bool ret;

                var rangeSupported = response.Content.Headers.ContentRange?.HasRange == true;

                var success = UpdateChunks(download, chunk, rangeSupported);
                if (success) ret = success;
                else
                {
                    chunk.Source!.Priority = int.MaxValue;
                    response.Dispose();
                    success = false;

                    ret = success;
                }

                if (!ret)
                {
                    return;
                }
                var start = DateTime.Now;
                await using var stream = await response.Content.ReadAsStreamAsync(cancel);
                await ReadStreamToEnd(job, stream, chunk, writes, cancel);
                _logger.LogDebug("chunk {}-{} @ {} took {} ms => {} kb/s",
                    chunk.Offset, chunk.Offset + chunk.Size, chunk.Source, (int)((DateTime.Now - start).TotalMilliseconds), chunk.KBytesPerSecond);
            }
        }

        private Source TakeSource(DownloadState download)
        {
            lock (download)
            {
                var res = download.Sources?.Aggregate((prev, iter) => iter.Priority < prev.Priority ? iter : prev);
                if ((res == null) || (res.Priority == int.MaxValue))
                {
                    throw new InvalidOperationException("no valid download sources");
                }
                res.Priority += download.Sources?.Count() ?? 0;
                return res;
            }
        }

        private async Task ReadStreamToEnd(DLJob job, Stream stream, ChunkState chunk, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            var lastRead = -1;
            var offset = chunk.Offset + chunk.Completed;
            chunk.Started = DateTime.Now;
            while (lastRead != 0)
            {
                var rented = _memoryPool.Rent((int)ReadBlockSize.Value);
                var (filledBuffer, totalRead) = await FillBuffer(stream, rented, (int)chunk.RemainingToRead, cancel);
                lastRead = totalRead;
                
                await job.ReportAsync(Size.FromLong(lastRead), cancel);
                if (lastRead > 0)
                {
                    chunk.Read += lastRead;

                    await writes.WriteAsync(new WriteOrder
                    {
                        Offset = offset,
                        Data = filledBuffer,
                        Owner = rented,
                    }, CancellationToken.None);
                    offset += lastRead;
                }
            }
        }

        /// <summary>
        /// Tries to fill the buffer with data from the stream. Returns the actual buffer once it's full or the stream
        /// has ended.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        /// <param name="totalSize"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async ValueTask<(Memory<byte> Buffer, int TotalRead)> FillBuffer(Stream stream, IMemoryOwner<byte> data, int totalSize, CancellationToken token)
        {
            var memory = data.Memory;
            var totalRead = 0;
            while (totalRead < data.Memory.Length && totalRead < totalSize)
            {
                var read = await stream.ReadAsync(memory[totalRead..], token);
                if (read == 0)
                    break;

                totalRead += read;
            }
            return (memory[..totalRead], totalRead);
        }

        #endregion // Downloading

        #region Header Handling
        
        private bool UpdateChunks(DownloadState download, ChunkState chunk, bool rangeSupport)
        {
            if (rangeSupport)
            {
                // range request supported, perfect
                return true;
            }

            if (chunk.Completed > 0)
            {
                // we've already started a chunked download but this source doesn't support it. Drop the source,
                // cancel the thread
                _logger.LogInformation("Source ignored because it doesn't support chunked downloads: {Source}", chunk.Source);
                return false;
            }

            // TODO we should try if one of the other sources supports ranges
            _logger.LogInformation("Source doesn't support chunked downloads, downloading in all-or-nothing mode: {Source}", chunk.Source);
            // this will set data.size negative if the Content-Length header is also missing
            chunk.Size = download.TotalSize;
            download.Chunks = new List<ChunkState> { chunk };

            return true;
        }

        #endregion // Header Handling

        #region Download State Persistance

        private async Task<DownloadState> InitiateState(AbsolutePath destination, Size size, IReadOnlyList<HttpRequestMessage> sourceMessages, CancellationToken cancel)
        {
            
            DownloadState? state = null;
            var stateFilePath = DownloadState.GetStateFilePath(destination);
            if (stateFilePath.FileExists && DownloadState.GetTempFilePath(destination).FileExists)
            {
                _logger.LogInformation("Resuming prior download {FilePath}", destination);
                state = DeserializeDownloadState(await stateFilePath.ReadAllTextAsync(cancel));
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
                for (var offset = Size.Zero; offset < size; offset += ChunkSize)
                {
                    chunks.Add(new ChunkState
                    {
                        Offset = (long)offset.Value,
                        Size = (long)Math.Min(ChunkSize.Value, size.Value - offset.Value),
                        Completed = 0,
                        Read = 0
                    });
                }
                
                _logger.LogInformation("Starting download {FilePath} in {ChunkCount} chunks of {Size} ", destination, chunks.Count, ChunkSize);
                
                state = new DownloadState
                {
                    TotalSize = (long)size.Value,
                    Sources = sources.ToArray(),
                    Destination = destination,
                    Chunks = chunks
                };
            }
            
            return state;
        }
        
        private async Task WriteDownloadState(DownloadState state, CancellationToken cancel = default)
        {
            await using var fs = state.StateFilePath.Create();
            try
            {
                await fs.WriteAsync(Encoding.UTF8.GetBytes(SerializeDownloadState(state)), cancel);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to write download state");
            }
        }

        private string SerializeDownloadState(DownloadState state)
        {
            return JsonSerializer.Serialize(state);
        }

        private DownloadState DeserializeDownloadState(string input)
        {
            var res = JsonSerializer.Deserialize<DownloadState>(input);
            if (res == null)
            {
                return new DownloadState();
            }
            return res;
        }

        #endregion // Download State Persistence

        #region Utility Functions



        private RangeHeaderValue MakeRangeHeader(ChunkState chunk)
        {
            var from = chunk.Offset + chunk.Read;
            long? to = null;
            if (chunk.Size > 0)
            {
                to = chunk.Offset + chunk.Size - 1;
            }
            return new RangeHeaderValue(from, to);
        }

        #endregion // Utility Functions
    }
}
