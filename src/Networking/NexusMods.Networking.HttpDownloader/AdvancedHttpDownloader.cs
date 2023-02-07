using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using Noggog;
using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace NexusMods.Networking.HttpDownloader
{
    /**
     * HTTP downloader with the following features
     *  - pause/resume support
     *  - support for downloading in multiple parallel requests
     *  - automatically switch to alternative source in case of server error
     *
     * Workflow:
     *   - A write job is started that opens the output file for writing and waits for write orders from the
     *     downloaders
     *   - Before the download can be chunked, an initial request to the server has to be made to determine
     *     if range requests are allowed and how large the file is. This chunk shouldn't be too small, chunking tiny
     *     files is pointless overhead
     *   - Once this initial response is received, the remaining file size, if any, is broken up into chunks based on
     *     a chunking strategy
     *   - worker jobs are generated to handle chunks
     *   - the hash is generated once the whole file is downloaded
     *
     * TODO:
     *   - decide on a chunking strategy. One per source? Fixed number of chunks? Fixed size?
     *   - make paramaters configurable
     *   - bandwidth throttling
     *   - currently only checks the first server for range request support, if it doesn't have it we just continue that
     *     download as an all-or-nothing fetch
     *   - measure performance for individual chunks
     *   - if there are multiple sources available, cancel slow chunks and resume with a different source
     *   - currently we only do one request to the first source to determine if resume is supported. If it doesn't, we
     *     could check if the others do, but then we might be causing several unnecessary requests every time if none of
     *     them do
     */
    public class AdvancedHttpDownloader : IHttpDownloader
    {
        private readonly ILogger<AdvancedHttpDownloader> _logger;
        private readonly HttpClient _client;
        private readonly IResource<IHttpDownloader, Size> _limiter;

        private const int INIT_CHUNK_SIZE = 1 * 1024 * 1024;
        private const int READ_BLOCK_SIZE = 1024 * 1024;
        private const int CHUNK_COUNT = 4;
        private const int WRITE_QUEUE_LENGTH = 10;

        public AdvancedHttpDownloader(ILogger<AdvancedHttpDownloader> logger, HttpClient client, IResource<IHttpDownloader, Size> limiter)
        {
            _logger = logger;
            _client = client;
            _limiter = limiter;
        }

        public async Task<Hash> Download(IReadOnlyList<HttpRequestMessage> sources, AbsolutePath destination, Size? size, CancellationToken cancel)
        {
            DownloadState? state = null;

            var primaryJob = await _limiter.Begin($"Initiating Download {destination.FileName}", size ?? Size.One, cancel);

            state = await InitiateState(destination, cancel);
            state.sources = new PriorityQueue<HttpRequestMessage, int>(
                sources.Select<HttpRequestMessage, (HttpRequestMessage, int)>((HttpRequestMessage source, int idx) => new(source, idx)));
            state.destination = destination;

            var writeQueue = Channel.CreateBounded<WriteOrder>(WRITE_QUEUE_LENGTH);

            var fileWriter = FileWriterTask(state, writeQueue.Reader, primaryJob, cancel);

            await DownloaderTask(state, writeQueue.Writer, cancel);

            // once the download driver is done (for whatever reason), signal the file writer to finish the rest of the queue and then also end
            writeQueue.Writer.Complete();
            await fileWriter;

            var algo = new xxHashAlgorithm(0);
            return Hash.From(algo.HashBytes(await destination.ReadAllBytesAsync(cancel)));
        }

        private async Task<DownloadState> InitiateState(AbsolutePath destination, CancellationToken cancel)
        {
            var tempPath = TempFilePath(destination);
            AbsolutePath stateFilePath = StateFilePath(destination);

            DownloadState? state = null;
            if (tempPath.FileExists && stateFilePath.FileExists)
            {
                _logger.LogInformation("Resuming prior download {filePath}", destination.ToString());
                state = DeserializeDownloadState(await stateFilePath.ReadAllTextAsync(cancel));
            }

            if (state == null)
            {
                _logger.LogInformation("Starting download {filePath}", destination.ToString());
                state = new DownloadState();
            }

            if (state.chunks.Count == 0)
            {
                // at this point we don't know how large the file is in total and we don't know if the source supports range requests
                // so start with a small requests for the first part of the file before we can decide if/how to chunk the download
                state.chunks.Add(CreateChunk(0, INIT_CHUNK_SIZE, true));
            }

            return state;
        }

        private async Task FileWriterTask(DownloadState state, ChannelReader<WriteOrder> writes, IJob<IHttpDownloader, Size> job, CancellationToken cancel)
        {
            var tempPath = TempFilePath(state.destination);

            using (var file = tempPath.Open(FileMode.OpenOrCreate, FileAccess.Write))
            {
                if ((state.totalSize > 0) && (file.Length != state.totalSize))
                {
                    file.SetLength(state.totalSize);
                }

                while (await writes.WaitToReadAsync(cancel))
                {
                    var order = await writes.ReadAsync();
                    await ApplyWriteOrder(file, order, state);
                    await job.Report(Size.From(order.size), cancel);
                }
            }

            if (!HasUnfinishedChunk(state))
            {
                StateFilePath(state.destination).Delete();
                File.Move(tempPath.ToString(), state.destination.ToString(), true);
            }
        }

        private async Task DownloaderTask(DownloadState state, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            var unfinishedChunks = state.chunks.Where(chunk => chunk.completed < chunk.size).ToList();

            if (state.totalSize < 0)
            {
                // if we don't know the total size that means we've never received a response from the server so we don't know
                // if chunking is even possible. For now only download the first chunk on the primary job until we know more
                await ProcessChunk(state, unfinishedChunks.First(), writes, cancel);
                unfinishedChunks = state.chunks.Where(chunk => chunk.completed < chunk.size).ToList();
            }

            await Task.WhenAll(unfinishedChunks.Select(async chunk =>
            {
                var chunkJob = await _limiter.Begin($"Download Chunk ${chunk.offset}", Size.From(chunk.size), cancel);
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
                    download.sources = FilterSource(download.sources, source);
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

            if ((response == null) || (!chunk.initChunk && (response.Content.Headers.ContentRange?.HasRange != true)))
            {
                lock (download)
                {
                    download.sources = FilterSource(download.sources, source);
                }

                await ProcessChunk(download, chunk, writes, cancel);
            } else if (HandleServerResponse(download, ref chunk, source, response))
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancel);
                await ReadStreamToEnd(stream, chunk.offset + chunk.completed, writes, cancel);
            }
        }

        private async Task ReadStreamToEnd(Stream stream, long offset, ChannelWriter<WriteOrder> writes, CancellationToken cancel)
        {
            int lastRead = -1;
            while (lastRead != 0)
            {
                byte[] buffer = new byte[READ_BLOCK_SIZE];
                lastRead = await stream.ReadAsync(buffer, 0, READ_BLOCK_SIZE, cancel);
                if (lastRead > 0)
                {
                    await writes.WriteAsync(new WriteOrder
                    {
                        offset = offset,
                        size = lastRead,
                        data = buffer,
                    }, cancel);
                    offset += lastRead;
                }
            }
        }

        private void HandleFirstServerResponse(DownloadState download, ChunkState initChunk, bool rangeSupported)
        {
            if (download.totalSize > 0)
            {
                // TODO chunking strategy? Currently uses fixed chunk count, variable size (like Vortex)
                long remainingSize = download.totalSize - initChunk.size;
                long chunkSize = (long)MathF.Ceiling((float)remainingSize / CHUNK_COUNT);

                long curOffset = initChunk.size;
                for (int i = 0; i < CHUNK_COUNT; ++i)
                {
                    if (curOffset >= download.totalSize)
                    {
                        break;
                    }
                    long curSize = Math.Min(chunkSize, download.totalSize - curOffset);
                    download.chunks.Add(CreateChunk(curOffset, curSize));
                    curOffset += curSize;
                }
            }
            else if (rangeSupported)
            {
                // if we don't know the total size we currently don't do chunking but resume might still be possible
                download.chunks.Add(CreateChunk(initChunk.size, -1));
            }
        }

        private void UpdateTotalSize(DownloadState download, HttpResponseMessage response)
        {
            if (download.totalSize > 0)
            {
                // don't change size once we've decided on one. That would almost certainly be a bug and the rest of
                // our code won't deal well with the download size changing
                return;
            }

            if (response.Content.Headers.ContentRange?.HasRange == true)
            {
                // in a range response, the Content-Length header contains the size of the chunk, not the size of the file
                download.totalSize = response.Content.Headers.ContentRange?.Length ?? -1;
            }
            else
            {
                download.totalSize = response.Content.Headers.ContentLength ?? -1;
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

                if (chunk.initChunk && (chunk.completed == 0))
                {
                    // TODO we should try if one of the other sources supports ranges

                    _logger.LogInformation("Source doesn't support chunked downloads, downloading in all-or-nothing mode: {Source}", source.RequestUri);
                    // this will set data.size negative if the Content-Length header is also missing
                    chunk.size = download.totalSize;
                    download.chunks = new List<ChunkState> { chunk };
                }
                else
                {
                    // we've already started a chunked download but this source doesn't support it. Drop the source,
                    // cancel the thread
                    _logger.LogInformation("Source ignored because it doesn't support chunked downloads: {Source}", source.RequestUri);
                    download.sources = FilterSource(download.sources, source);
                    response.Dispose();
                    success = false;
                }
            }

            if (chunk.initChunk && (download.chunks.Count == 1))
            {
                HandleFirstServerResponse(download, chunk, rangeSupported);
            }

            return success;
        }

        #region Progress Info Serialization

        private async Task WriteDownloadState(DownloadState state, CancellationToken cancel = default)
        {
            using var fs = File.OpenWrite(StateFilePath(state.destination).ToString());
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
                completed = 0,
                initChunk = initChunk,
                size = size,
                offset = start,
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
            return state.chunks.Any(chunk => chunk.completed < chunk.size);
        }

        private async Task DoWriteOrder(Stream file, WriteOrder order)
        {
            file.Position = order.offset;
            await file.WriteAsync(order.data, 0, order.size);
        }
        private async Task ApplyWriteOrder(Stream file, WriteOrder order, DownloadState state)
        {
            if (order.data == null)
            {
                file.SetLength(order.size);
            }
            else
            {
                await DoWriteOrder(file, order);
                UpdateChunk(state, order);

                // important: data file has to be flushed before updating the state file. If the state is outdated that's ok, on resume
                //   we'll just download a bit more than necessary. But the other way around the output file would be corrupted.
                //   if this is a performance issue, either increase the block size or tie the state update to the automatic file flushing
                //   (something like file.OnFlush(() => WriteDownloadState(state))
                await file.FlushAsync();

                await WriteDownloadState(state);
            }
        }

        private void UpdateChunk(DownloadState state, WriteOrder order)
        {
            lock (state)
            {
                int chunkIdx = state.chunks.FindIndex(chunk =>
                    (chunk.offset <= order.offset)
                    && ((chunk.size == -1)
                        || (order.offset < (chunk.offset + chunk.size))));

                state.chunks[chunkIdx] = state.chunks[chunkIdx].Progress(order.size, null);
            }
        }

        private HttpRequestMessage TakeSource(DownloadState download)
        {
            HttpRequestMessage source;
            int priority;

            lock (download)
            {
                if (!download.sources.TryDequeue(out source, out priority))
                {
                    throw new InvalidOperationException("no download source provided");
                }

                // reduce priority of source so that other sources are used for further chunks
                download.sources.Enqueue(source, priority + download.sources.Count);
            }

            return source;
        }

        private RangeHeaderValue MakeRangeHeader(ChunkState chunk)
        {
            long from = chunk.offset + chunk.completed;
            long? to = null;
            if (chunk.size > 0)
            {
                to = chunk.offset + chunk.size;
            }
            return new RangeHeaderValue(from, to);
        }

        #endregion
    }

    #region State Structs
    class ChunkState
    {
        public long offset { get; set; }
        public long size { get; set; }
        public long completed { get; set; }
        public bool initChunk { get; set; }
        public string source { get; set; }

        public ChunkState Progress(long distance, string? source)
        {
            return new ChunkState
            {
                offset = offset,
                size = size,
                completed = completed + distance,
                initChunk = initChunk,
                source = source ?? "N/A"
            };
        }
    }

    struct WriteOrder
    {
        public long offset;
        public int size;
        public byte[] data;
    }

    class DownloadState
    {
        public long totalSize { get; set; } = -1;

        public List<ChunkState> chunks { get; set; } = new List<ChunkState>();

        // [JsonIgnore]
        public AbsolutePath destination;

        // [JsonIgnore]
        public PriorityQueue<HttpRequestMessage, int> sources = new PriorityQueue<HttpRequestMessage, int>();
    }

    #endregion // State Structs

}
