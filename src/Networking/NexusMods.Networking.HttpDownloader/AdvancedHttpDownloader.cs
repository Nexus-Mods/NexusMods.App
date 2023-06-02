using System.Buffers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
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
        private const int MiB = 1024 * 1024;
        private const int ReadBlockSize = 1 * MiB;
        private const int InitChunkSize = ReadBlockSize;

        // see IHttpDownloaderSettings for docs about these
        private readonly int _chunkCount;
        private readonly int _writeQueueLength;
        private readonly int _minCancelAge;
        private readonly double _cancelSpeedFraction;

        // the shared pool is capped at 1MB (2^20 bytes) per buffer so if larger blocks are desired, use a custom pool
        private readonly ArrayPool<byte> _bufferPool;

        /// <summary>
        /// Constructor
        /// </summary>
        public AdvancedHttpDownloader(ILogger<AdvancedHttpDownloader> logger, HttpClient client, IResource<IHttpDownloader, Size> limiter, IHttpDownloaderSettings settings)
        {
            _logger = logger;
            _client = client;
            _limiter = limiter;
            _chunkCount = settings.ChunkCount;
            _writeQueueLength = settings.WriteQueueLength;
            _minCancelAge = settings.MinCancelAge;
            _cancelSpeedFraction = settings.CancelSpeedFraction;
#pragma warning disable CS0162
            // ReSharper disable once HeuristicUnreachableCode
            _bufferPool = (ReadBlockSize <= 1 * MiB)
                ? ArrayPool<byte>.Shared
                : ArrayPool<byte>.Create(ReadBlockSize, _writeQueueLength * 2);
#pragma warning restore CS0162
        }

        /// <inheritdoc />
        public async Task<Hash> DownloadAsync(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, HttpDownloaderState? downloaderState, Size? size, CancellationToken cancel)
        {
            downloaderState ??= new HttpDownloaderState();
            using var primaryJob = await _limiter.BeginAsync($"Initiating Download {destination.FileName}", size ?? Size.One, cancel);
            // Note: All data eventually is piped into primary job (when writing to destination), so we can just use that to track everything as a whole.
            downloaderState.Jobs.Add(primaryJob);
            
            // Integrate local download.
            var hash = await LocalFileDownloader.TryDownloadLocal(primaryJob, sources, destination);
            if (hash != null)
                return hash.Value;
            
            var state = await InitiateState(destination, cancel);
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
            var tempPath = TempFilePath(state.Destination);

            if (!HasIncompleteChunk(state))
            {
                StateFilePath(state.Destination).Delete();
                File.Move(tempPath.ToString(), state.Destination.ToString(), true);
                return await state.Destination.XxHash64Async(cancel);
            }

            return Hash.Zero;
        }

        #region Output Writing

        struct OrderLease : IDisposable
        {
            public WriteOrder Order { get; private set; }
            private readonly ArrayPool<byte> _bufferPool;

            public static async Task<OrderLease> Lend(ChannelReader<WriteOrder> writes, ArrayPool<byte> pool)
            {
                return new OrderLease(await writes.ReadAsync(CancellationToken.None), pool);
            }

            private OrderLease(WriteOrder order, ArrayPool<byte> pool)
            {
                Order = order;
                _bufferPool = pool;
            }

            public void Dispose()
            {
                _bufferPool.Return(Order.Data);
            }
        }

        private async Task FileWriterTask(DownloadState state, ChannelReader<WriteOrder> writes, DLJob job, CancellationToken cancel)
        {
            var tempPath = TempFilePath(state.Destination);

            var sizeKnown = false;

            await using var file = tempPath.Open(FileMode.OpenOrCreate, FileAccess.Write);
            while (await writes.WaitToReadAsync(cancel))
            {
                // writing out data that was already downloaded is intentionally not cancellable. The writes channel
                // will be closed on cancellation so this loop will definitively end once all remaining blocks have
                // been written
                using var order = await OrderLease.Lend(writes, _bufferPool);
                // var order = await writes.ReadAsync(CancellationToken.None);
                if (!sizeKnown && state.TotalSize > 0)
                {
                    // technically we don't need this as the FileStream is happy to resize the file as necessary
                    // as we seek around but that's not true for Streams in general plus it feels cleaner to
                    // allocate the space right away
                    file.SetLength(state.TotalSize);
                    sizeKnown = true;
                }
                await ApplyWriteOrder(file, order.Order, state);
                // _bufferPool.Return(order.Data);
                await job.ReportAsync(Size.FromLong(order.Order.Size), cancel);
            }
        }

        private async Task DoWriteOrder(Stream file, WriteOrder order)
        {
            file.Position = order.Offset;
            await file.WriteAsync(order.Data, 0, order.Size, CancellationToken.None);
        }

        private async Task ApplyWriteOrder(Stream file, WriteOrder order, DownloadState state)
        {
            await DoWriteOrder(file, order);
            UpdateChunk(state, order);

            // data file has to be flushed before updating the state file. If the state is outdated that's ok, on resume
            //   we'll just download a bit more than necessary. But the other way around the output file would be corrupted.
            //   if this is a performance issue, either increase the block size or tie the state update to the automatic file flushing
            //   (something like file.OnFlush(() => WriteDownloadState(state))
            await file.FlushAsync(CancellationToken.None);

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

                state.Chunks[chunkIdx].Completed += order.Size;
            }
        }

        #endregion

        #region Downloading

        private async Task DownloaderTask(DLJob job, DownloadState state, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            if (state.TotalSize < 0)
            {
                // if we don't know the total size that means we've never received a response from the server so we don't know
                // if chunking is even possible. For now only download the first chunk on the primary job until we know more
                var firstChunk = UnfinishedChunks(state).First();
                await DownloadChunk(job, state, firstChunk, writes, cancel);
            }

            var finishReward = 1024;

            // start one task per unfinished chunk. We never start additional tasks but a task may take on another chunks
            await Task.WhenAll(UnfinishedChunks(state).Select(async chunk =>
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
            var slowChunk = FindSlowChunk(UnfinishedChunks(state), chunk.Source!, chunk.BytesPerSecond);
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
                Size = slowChunk.Size - slowChunk.Read,
                InitChunk = false,
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
            var request = CopyRequest(chunk.Source!.Request!);
            request.Headers.Range = MakeRangeHeader(chunk);

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);

            if (!response.IsSuccessStatusCode)
            {
                // TODO deal with redirects

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
                sourceValid = (response != null)
                            && (chunk.InitChunk || response.Content.Headers.ContentRange?.HasRange == true);
                if (!sourceValid)
                {
                    chunk.Source.Priority = int.MaxValue;
                }
            }

            if ((response != null) && HandleServerResponse(download, chunk, response))
            {
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
                var buffer = _bufferPool.Rent(ReadBlockSize);
                lastRead = await stream.ReadAsync(buffer, 0, ReadBlockSize, cancel);
                job.ReportNoWait(Size.FromLong(lastRead));
                if (lastRead > 0)
                {
                    chunk.Read += lastRead;

                    await writes.WriteAsync(new WriteOrder
                    {
                        Offset = offset,
                        Size = lastRead,
                        Data = buffer,
                    }, CancellationToken.None);
                    offset += lastRead;
                }
            }
        }

        #endregion // Downloading

        #region Header Handling

        private void HandleFirstServerResponse(DownloadState download, ChunkState initChunk, bool rangeSupported)
        {
            if (download.TotalSize > 0)
            {
                // TODO chunking strategy? Currently uses fixed chunk count, variable size (like Vortex)
                var remainingSize = download.TotalSize - initChunk.Size;
                var chunkSize = (long)MathF.Ceiling((float)remainingSize / _chunkCount);

                var curOffset = initChunk.Size;
                for (var i = 0; i < _chunkCount; ++i)
                {
                    if (curOffset >= download.TotalSize)
                    {
                        break;
                    }
                    var curSize = Math.Min(chunkSize, download.TotalSize - curOffset);
                    download.Chunks.Add(CreateChunk(curOffset, curSize));
                    curOffset += curSize;
                }
            }
            else if (rangeSupported)
            {
                // if we don't know the total size we currently don't do chunking but resume might still be possible
                download.Chunks.Add(CreateChunk(initChunk.Size, -1));
            }
        }

        private bool HandleServerResponse(DownloadState download, ChunkState chunk, HttpResponseMessage response)
        {
            UpdateTotalSize(download, response);

            var rangeSupported = response.Content.Headers.ContentRange?.HasRange == true;

            var success = UpdateChunks(download, chunk, rangeSupported);
            if (!success)
            {
                chunk.Source!.Priority = int.MaxValue;
                response.Dispose();
                success = false;
            }

            if (chunk.InitChunk && (download.Chunks.Count == 1))
            {
                HandleFirstServerResponse(download, chunk, rangeSupported);
            }

            return success;
        }

        private bool UpdateChunks(DownloadState download, ChunkState chunk, bool rangeSupport)
        {
            if (rangeSupport)
            {
                // range request supported, perfect
                return true;
            }

            if (!chunk.InitChunk || (chunk.Completed > 0))
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

        private void UpdateTotalSize(DownloadState download, HttpResponseMessage response)
        {
            if (download.TotalSize > 0)
            {
                // don't change size once we've decided on one. That would almost certainly be a bug and the rest of
                // our code won't deal well with the download size changing
                return;
            }

            if (response.Content.Headers.ContentRange?.HasRange == true)
            {
                // in a range response, the Content-Length header contains the size of the chunk, not the size of the file
                download.TotalSize = response.Content.Headers.ContentRange?.Length ?? -1;
            }
            else
            {
                download.TotalSize = response.Content.Headers.ContentLength ?? -1;
            }
            foreach (var chunk in download.Chunks)
            {
                if (chunk.Offset + chunk.Size > download.TotalSize)
                {
                    chunk.Size = download.TotalSize - chunk.Offset;
                }
            }
        }

        #endregion // Header Handling

        #region Download State Persistance

        private async Task<DownloadState> InitiateState(AbsolutePath destination, CancellationToken cancel)
        {
            var tempPath = TempFilePath(destination);
            var stateFilePath = StateFilePath(destination);

            DownloadState? state = null;
            if (tempPath.FileExists && stateFilePath.FileExists)
            {
                _logger.LogInformation("Resuming prior download {FilePath}", destination);
                state = DeserializeDownloadState(await stateFilePath.ReadAllTextAsync(cancel));
            }

            if (state != null)
            {
                NormalizeChunks(state);
            }
            else
            {
                _logger.LogInformation("Starting download {FilePath}", destination);
                state = new DownloadState();
            }

            if (state.Chunks.Count == 0)
            {
                // at this point we don't know how large the file is in total and we don't know if the source supports range requests
                // so start with a small requests for the first part of the file before we can decide if/how to chunk the download
                state.Chunks.Add(CreateChunk(0, InitChunkSize, true));
            }

            return state;
        }

        private void NormalizeChunks(DownloadState state)
        {
            // normalize input chunks so we don't fail to resume if the previous download was interrupted at a bad time
            state.Chunks = state.Chunks.Select(chunk => new ChunkState
            {
                Completed = chunk.Completed,
                InitChunk = chunk.InitChunk,
                Offset = chunk.Offset,
                Read = chunk.Completed,
                Size = chunk.Size,
            }).ToList();
        }

        private async Task WriteDownloadState(DownloadState state, CancellationToken cancel = default)
        {
            await using var fs = StateFilePath(state.Destination).Create();
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

        private IEnumerable<ChunkState> UnfinishedChunks(DownloadState state)
        {
            if (state.TotalSize < 0)
            {
                return state.Chunks.Where(chunk => chunk.Read < chunk.Size);
            }

            return state.Chunks.Where(chunk => (chunk.Read < chunk.Size) && ((chunk.Offset + chunk.Read) < state.TotalSize));
        }
        private bool HasIncompleteChunk(DownloadState state)
        {
            if (state.TotalSize < 0)
            {
                return state.Chunks.Any(chunk => chunk.Completed < chunk.Size);
            }

            return state.Chunks.Any(chunk => (chunk.Completed < chunk.Size) && ((chunk.Offset + chunk.Completed) < state.TotalSize));
        }

        private AbsolutePath StateFilePath(AbsolutePath input)
        {
            return input.ReplaceExtension(new Extension(".progress"));
        }

        private AbsolutePath TempFilePath(AbsolutePath input)
        {
            return input.ReplaceExtension(new Extension(".downloading"));
        }

        private ChunkState CreateChunk(long start, long size, bool initChunk = false)
        {
            return new ChunkState
            {
                Completed = 0,
                Read = 0,
                InitChunk = initChunk,
                Size = size,
                Offset = start,
            };
        }

        private HttpRequestMessage CopyRequest(HttpRequestMessage input)
        {
            var newRequest = new HttpRequestMessage(input.Method, input.RequestUri);
            foreach (var option in input.Options)
            {
                newRequest.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            }

            foreach (var header in input.Headers)
            {
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return newRequest;
        }

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

    #region State Structs
    class ChunkState
    {
        public long Offset { get; init; }
        public long Size { get; set; }
        [JsonIgnore]
        public long Read { get; set; }
        public long Completed { get; set; }
        public bool InitChunk { get; init; }

        [JsonIgnore]
        public Source? Source { get; set; }

        [PublicAPI]
        public string SourceUrl => Source?.Request?.RequestUri?.AbsoluteUri ?? "No URL";

        [JsonIgnore]
        public CancellationTokenSource? Cancel { get; set; }
        [JsonIgnore]
        public DateTime Started { get; set; }

        public int BytesPerSecond => (int)Math.Floor(Read / (DateTime.Now - Started).TotalSeconds);

        [JsonIgnore]
        public int KBytesPerSecond => (int)Math.Floor((Read / (DateTime.Now - Started).TotalSeconds) / 1024);
    }

    struct WriteOrder
    {
        public long Offset;
        public int Size;
        public byte[] Data;
    }

    class Source
    {
        public HttpRequestMessage? Request { get; init; }
        public int Priority { get; set; }
        public override string ToString()
        {
            return Request?.RequestUri?.AbsoluteUri ?? "No URL";
        }
    }

    class DownloadState
    {
        public long TotalSize { get; set; } = -1;

        public List<ChunkState> Chunks { get; set; } = new List<ChunkState>();

        [JsonIgnore]
        public AbsolutePath Destination;

        [JsonIgnore]
        public Source[]? Sources;
    }

    #endregion // State Structs
}
