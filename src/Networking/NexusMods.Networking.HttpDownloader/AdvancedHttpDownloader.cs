using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Noggog;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace NexusMods.Networking.HttpDownloader
{
    /// <summary>
    /// HTTP downloader with the following features<br/>
    ///  - pause/resume support<br/>
    ///  - support for downloading in multiple parallel requests<br/>
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
    ///   - Once this initial response is received, the remaining file size, if any, is broken up into chunks based on<br/>
    ///     a chunking strategy<br/>
    ///   - worker jobs are generated to handle chunks<br/>
    ///   - the hash is generated once the whole file is downloaded<br/>
    ///
    /// TODO:<br/>
    ///   - decide on a chunking strategy. One per source? Fixed number of chunks? Fixed size?<br/>
    ///   - make paramaters configurable<br/>
    ///   - bandwidth throttling<br/>
    ///   - currently only checks the first server for range request support, if it doesn't have it we just continue that<br/>
    ///     download as an all-or-nothing fetch<br/>
    ///   - measure performance for individual chunks<br/>
    ///   - if there are multiple sources available, cancel slow chunks and resume with a different source<br/>
    ///   - currently we only do one request to the first source to determine if resume is supported. If it doesn't, we<br/>
    ///     could check if the others do, but then we might be causing several unnecessary requests every time if none of<br/>
    ///     them do<br/>
    /// </remarks>
    public class AdvancedHttpDownloader : IHttpDownloader
    {
        private readonly ILogger<AdvancedHttpDownloader> _logger;
        private readonly HttpClient _client;
        private readonly IResource<IHttpDownloader, Size> _limiter;

        private const int InitChunkSize = 1 * 1024 * 1024;
        private const int ReadBlockSize = 1024 * 1024;
        private const int ChunkCount = 4;
        private const int WriteQueueLength = 10;

        public AdvancedHttpDownloader(ILogger<AdvancedHttpDownloader> logger, HttpClient client, IResource<IHttpDownloader, Size> limiter)
        {
            _logger = logger;
            _client = client;
            _limiter = limiter;
        }

        /// <inheritdoc />
        public async Task<Hash> Download(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, Size? size, CancellationToken cancel)
        {
            DownloadState? state = null;

            var primaryJob = await _limiter.Begin($"Initiating Download {destination.FileName}", size ?? Size.One, cancel);

            state = await InitiateState(destination, cancel);
            state.Sources = new PriorityQueue<HttpRequestMessage, int>(
                sources.Select<HttpRequestMessage, (HttpRequestMessage, int)>((HttpRequestMessage source, int idx) => new(source, idx)));
            state.Destination = destination;

            var writeQueue = Channel.CreateBounded<WriteOrder>(WriteQueueLength);

            var fileWriter = FileWriterTask(state, writeQueue.Reader, primaryJob, cancel);

            await DownloaderTask(state, writeQueue.Writer, cancel);

            // once the download driver is done (for whatever reason), signal the file writer to finish the rest of the queue and then also end
            writeQueue.Writer.Complete();
            await fileWriter;

            return await destination.XxHash64(cancel);
        }

        private async Task<DownloadState> InitiateState(AbsolutePath destination, CancellationToken cancel)
        {
            var tempPath = TempFilePath(destination);
            AbsolutePath stateFilePath = StateFilePath(destination);

            DownloadState? state = null;
            if (tempPath.FileExists && stateFilePath.FileExists)
            {
                _logger.LogInformation("Resuming prior download {filePath}", destination);
                state = DeserializeDownloadState(await stateFilePath.ReadAllTextAsync(cancel));
            }

            if (state == null)
            {
                _logger.LogInformation("Starting download {filePath}", destination);
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

        private async Task FileWriterTask(DownloadState state, ChannelReader<WriteOrder> writes, IJob<IHttpDownloader, Size> job, CancellationToken cancel)
        {
            var tempPath = TempFilePath(state.Destination);

            bool sizeKnown = false;

            using (var file = tempPath.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                while (await writes.WaitToReadAsync(cancel))
                {
                    var order = await writes.ReadAsync();
                    if (!sizeKnown && state.TotalSize > 0)
                    {
                        // technically we don't need this as the FileStream is happy to resize the file as necessary
                        // as we seek around but that's not true for Streams in general plus it feels cleaner to
                        // allocate the space right away
                        file.SetLength(state.TotalSize);
                        sizeKnown = true;
                    }
                    await ApplyWriteOrder(file, order, state);
                    await job.Report(Size.From(order.Size), cancel);
                }
            }

            if (!HasUnfinishedChunk(state))
            {
                StateFilePath(state.Destination).Delete();
                File.Move(tempPath.ToString(), state.Destination.ToString(), true);
            }
        }

        private async Task DownloaderTask(DownloadState state, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            var unfinishedChunks = state.Chunks.Where(chunk => chunk.Completed < chunk.Size).ToList();

            if (state.TotalSize < 0)
            {
                // if we don't know the total size that means we've never received a response from the server so we don't know
                // if chunking is even possible. For now only download the first chunk on the primary job until we know more
                await ProcessChunk(state, unfinishedChunks.First(), writes, cancel);
                unfinishedChunks = state.Chunks.Where(chunk => chunk.Completed < chunk.Size).ToList();
            }

            await Task.WhenAll(unfinishedChunks.Select(async chunk =>
            {
                var chunkJob = await _limiter.Begin($"Download Chunk ${chunk.Offset}", Size.From(chunk.Size), cancel);
                await ProcessChunk(state, chunk, writes, cancel);
            }).ToArray());
        }

        private async Task<HttpResponseMessage?> SendRangeRequest(DownloadState download, ChunkState chunk, HttpRequestMessage source, CancellationToken cancel)
        {
            // Caller passes a full http request but we need to adjust the headers so need a copy
            var request = CopyRequest(source);
            request.Headers.Range = MakeRangeHeader(chunk);

            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);

            if (!response.IsSuccessStatusCode)
            {
                // TODO deal with redirects

                // TODO should be retrying the source once at least
                lock (download)
                {
                    download.Sources = FilterSource(download.Sources, source);
                }
                _logger.LogWarning("Failed to download {Source}: {StatusCode}", source.RequestUri, response.StatusCode);
                return null;
            }

            return response;
        }

        private async Task ProcessChunk(DownloadState download, ChunkState chunk, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            HttpRequestMessage? source = TakeSource(download);

            var response = await SendRangeRequest(download, chunk, source, cancel);

            if ((response == null) || (!chunk.InitChunk && (response.Content.Headers.ContentRange?.HasRange != true)))
            {
                lock (download)
                {
                    download.Sources = FilterSource(download.Sources, source);
                }

                await ProcessChunk(download, chunk, writes, cancel);
            } else if (HandleServerResponse(download, ref chunk, source, response))
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancel);
                await ReadStreamToEnd(stream, chunk.Offset + chunk.Completed, writes, cancel);
            }
        }

        private async Task ReadStreamToEnd(Stream stream, long offset, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            int lastRead = -1;
            while (lastRead != 0)
            {
                byte[] buffer = new byte[ReadBlockSize];
                lastRead = await stream.ReadAsync(buffer, 0, ReadBlockSize, cancel);
                if (lastRead > 0)
                {
                    await writes.WriteAsync(new WriteOrder
                    {
                        Offset = offset,
                        Size = lastRead,
                        Data = buffer,
                    }, cancel);
                    offset += lastRead;
                }
            }
        }

        private void HandleFirstServerResponse(DownloadState download, ChunkState initChunk, bool rangeSupported)
        {
            if (download.TotalSize > 0)
            {
                // TODO chunking strategy? Currently uses fixed chunk count, variable size (like Vortex)
                long remainingSize = download.TotalSize - initChunk.Size;
                long chunkSize = (long)MathF.Ceiling((float)remainingSize / ChunkCount);

                long curOffset = initChunk.Size;
                for (int i = 0; i < ChunkCount; ++i)
                {
                    if (curOffset >= download.TotalSize)
                    {
                        break;
                    }
                    long curSize = Math.Min(chunkSize, download.TotalSize - curOffset);
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
        }

        private bool HandleServerResponse(DownloadState download, ref ChunkState chunk, HttpRequestMessage source, HttpResponseMessage response)
        {
            UpdateTotalSize(download, response);

            bool rangeSupported = response.Content.Headers.ContentRange?.HasRange == true;

            bool success = true;

            if (!rangeSupported)
            {
                // no range request support

                if (chunk.InitChunk && (chunk.Completed == 0))
                {
                    // TODO we should try if one of the other sources supports ranges

                    _logger.LogInformation("Source doesn't support chunked downloads, downloading in all-or-nothing mode: {Source}", source.RequestUri);
                    // this will set data.size negative if the Content-Length header is also missing
                    chunk.Size = download.TotalSize;
                    download.Chunks = new List<ChunkState> { chunk };
                }
                else
                {
                    // we've already started a chunked download but this source doesn't support it. Drop the source,
                    // cancel the thread
                    _logger.LogInformation("Source ignored because it doesn't support chunked downloads: {Source}", source.RequestUri);
                    download.Sources = FilterSource(download.Sources, source);
                    response.Dispose();
                    success = false;
                }
            }

            if (chunk.InitChunk && (download.Chunks.Count == 1))
            {
                HandleFirstServerResponse(download, chunk, rangeSupported);
            }

            return success;
        }

        #region Progress Info Serialization

        private async Task WriteDownloadState(DownloadState state, CancellationToken cancel = default)
        {
            await using var fs = StateFilePath(state.Destination).Create();
            await fs.WriteAsync(Encoding.UTF8.GetBytes(SerializeDownloadState(state)), cancel);
        }

        private string SerializeDownloadState(DownloadState state)
        {
            return JsonSerializer.Serialize(state);
        }

        private DownloadState DeserializeDownloadState(string input)
        {
            DownloadState? res = JsonSerializer.Deserialize<DownloadState>(input);
            if (res == null)
            {
                return new DownloadState();
            }
            return res;
        }

        #endregion

        #region Utility Functions

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
                InitChunk = initChunk,
                Size = size,
                Offset = start,
            };
        }

        private PriorityQueue<HttpRequestMessage, int> FilterSource(PriorityQueue<HttpRequestMessage, int> queue, HttpRequestMessage item)
        {
            PriorityQueue<HttpRequestMessage, int> result = new PriorityQueue<HttpRequestMessage, int>();

            HttpRequestMessage? source;
            int priority;
            while (queue.TryDequeue(out source, out priority))
            {
                if (source != item)
                {
                    result.Enqueue(source, priority);
                }
            }
            return result;
        }

        private HttpRequestMessage CopyRequest(HttpRequestMessage input)
        {
            HttpRequestMessage newRequest = new HttpRequestMessage(input.Method, input.RequestUri);
            foreach (KeyValuePair<string, object?> option in input.Options)
            {
                newRequest.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in input.Headers)
            {
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return newRequest;
        }

        private bool HasUnfinishedChunk(DownloadState state)
        {
            return state.Chunks.Any(chunk => chunk.Completed < chunk.Size);
        }

        private async Task DoWriteOrder(Stream file, WriteOrder order)
        {
            file.Position = order.Offset;
            await file.WriteAsync(order.Data, 0, order.Size);
        }
        private async Task ApplyWriteOrder(Stream file, WriteOrder order, DownloadState state)
        {
            await DoWriteOrder(file, order);
            UpdateChunk(state, order);

            // data file has to be flushed before updating the state file. If the state is outdated that's ok, on resume
            //   we'll just download a bit more than necessary. But the other way around the output file would be corrupted.
            //   if this is a performance issue, either increase the block size or tie the state update to the automatic file flushing
            //   (something like file.OnFlush(() => WriteDownloadState(state))
            await file.FlushAsync();

            await WriteDownloadState(state);
        }

        private void UpdateChunk(DownloadState state, WriteOrder order)
        {
            lock (state)
            {
                int chunkIdx = state.Chunks.FindIndex(chunk =>
                    (chunk.Offset <= order.Offset)
                    && ((chunk.Size == -1)
                        || (order.Offset < (chunk.Offset + chunk.Size))));

                state.Chunks[chunkIdx] = state.Chunks[chunkIdx].Progress(order.Size);
            }
        }

        private HttpRequestMessage TakeSource(DownloadState download)
        {
            HttpRequestMessage? source;
            int priority;

            lock (download)
            {
                if (!download.Sources.TryDequeue(out source, out priority))
                {
                    throw new InvalidOperationException("no download source provided");
                }

                // reduce priority of source so that other sources are used for further chunks
                download.Sources.Enqueue(source, priority + download.Sources.Count);
            }

            return source;
        }

        private RangeHeaderValue MakeRangeHeader(ChunkState chunk)
        {
            long from = chunk.Offset + chunk.Completed;
            long? to = null;
            if (chunk.Size > 0)
            {
                to = chunk.Offset + chunk.Size;
            }
            return new RangeHeaderValue(from, to);
        }

        #endregion
    }

    #region State Structs
    class ChunkState
    {
        public long Offset { get; set; }
        public long Size { get; set; }
        public long Completed { get; set; }
        public bool InitChunk { get; set; }

        public ChunkState Progress(long distance)
        {
            return new ChunkState
            {
                Offset = Offset,
                Size = Size,
                Completed = Completed + distance,
                InitChunk = InitChunk,
            };
        }
    }

    struct WriteOrder
    {
        public long Offset;
        public int Size;
        public byte[] Data;
    }

    class DownloadState
    {
        public long TotalSize { get; set; } = -1;

        public List<ChunkState> Chunks { get; set; } = new List<ChunkState>();

        [JsonIgnore()]
        public AbsolutePath Destination;

        [JsonIgnore()]
        public PriorityQueue<HttpRequestMessage, int> Sources = new PriorityQueue<HttpRequestMessage, int>();
    }

    #endregion // State Structs

}
