using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class LocalHttpServer : IDisposable
{
    private readonly ILogger<LocalHttpServer> _logger;
    private readonly HttpListener _listener;
    private readonly string _prefix;

    public byte[] LargeData { get; set; }
    public Hash LargeDataHash { get; set; }


    public LocalHttpServer(ILogger<LocalHttpServer> logger)
    {
        _logger = logger;
        (_listener, _prefix) = CreateNewListener();
        _listener.Start();

        LargeData = GenerateLargeData();
        LargeDataHash = LargeData.AsSpan().XxHash64();

        StartLoop();
    }



    private byte[] GenerateLargeData()
    {
        var data = new byte[512 * 1024 * 1024];
        // Bit of a hack because .NextBytes is *extremely* slow
        var seed = Random.Shared.Next(0, 255);
        for (var offset = 0; offset < data.Length; offset++)
            data[offset] = (byte)((offset % 255) ^ seed);

        return data;
    }

    private void StartLoop()
    {
        Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                _logger.LogInformation("Got connection");
                using var resp = context.Response;

                if (context.Request.Url?.PathAndQuery.StartsWith("/Resources") ?? false)
                {
                    await ServeResource(resp, context.Request);
                }
                else
                {

                    switch (context.Request.Url?.PathAndQuery)
                    {
                        case "/hello":
                        {
                            var responseString = Encoding.UTF8.GetBytes("Hello World!");
                            resp.StatusCode = 200;
                            resp.StatusDescription = "OK";
                            resp.ProtocolVersion = HttpVersion.Version11;
                            resp.ContentLength64 = responseString.Length;
                            if (context.Request.HttpMethod != "HEAD")
                            {
                                await using var ros = resp.OutputStream;
                                await ros.WriteAsync(responseString);
                            }

                            break;
                        }
                        case "/reliable":
                            await HandleUnreliable(resp, context.Request, false);
                            break;
                        case "/unreliable":
                            await HandleUnreliable(resp, context.Request, true);
                            break;
                        default:
                        {
                            resp.StatusCode = 404;
                            resp.StatusDescription = "Not Found";
                            break;
                        }
                    }
                }
            }
        });
    }

    private async Task ServeResource(HttpListenerResponse resp, HttpListenerRequest request)
    {
        var filePath = Uri.UnescapeDataString(request.Url!.AbsolutePath);
        var fullPath = Path.GetFullPath("."+filePath);

        await using var stream = FileSystem.Shared.FromUnsanitizedFullPath(fullPath).Read();

        if (request.HttpMethod == "HEAD")
        {
            resp.StatusCode = (int)HttpStatusCode.OK;
            resp.StatusDescription = "OK";
            resp.ProtocolVersion = HttpVersion.Version11;
            resp.ContentLength64 = stream.Length;
            resp.Headers.Add(HttpResponseHeader.ContentType, "application/octet-stream");
            resp.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
            resp.Headers.Add(HttpResponseHeader.KeepAlive, "true");
            await using var _ = resp.OutputStream;
            return;
        }

        var rangeString = request.Headers.Get("Range");


        if (rangeString == null)
        {
            resp.StatusCode = (int)HttpStatusCode.OK;
            resp.StatusDescription = "OK";
            resp.ProtocolVersion = HttpVersion.Version11;
            resp.Headers.Add(HttpResponseHeader.ContentType, "application/octet-stream");
            resp.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
            resp.Headers.Add(HttpResponseHeader.KeepAlive, "true");
            resp.ContentLength64 = stream.Length;
            await using var ros = resp.OutputStream;
            await stream.CopyToAsync(ros);

        }
        else
        {
            var rangeValue = RangeHeaderValue.Parse(rangeString);
            var range = rangeValue.Ranges.First();
            resp.StatusCode = (int)HttpStatusCode.PartialContent;
            resp.StatusDescription = "Partial Content";
            resp.ProtocolVersion = HttpVersion.Version11;
            resp.Headers.Add(HttpResponseHeader.ContentType, "application/octet-stream");
            resp.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
            resp.Headers.Add(HttpResponseHeader.KeepAlive, "true");
            resp.Headers.Add(HttpResponseHeader.ContentRange, range.ToString());

            await SendContent(resp, stream, range);
        }
    }

    private const int MB = 1024 * 1024;
    private async Task HandleUnreliable(HttpListenerResponse resp, HttpListenerRequest request, bool truncate)
    {
        if (request.HttpMethod == "HEAD")
        {
            resp.StatusCode = (int)HttpStatusCode.OK;
            resp.StatusDescription = "OK";
            resp.ProtocolVersion = HttpVersion.Version11;
            resp.ContentLength64 = LargeData.Length;
            resp.Headers.Add(HttpResponseHeader.ContentType, "application/octet-stream");
            resp.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
            resp.Headers.Add(HttpResponseHeader.KeepAlive, "true");
            await using var _ = resp.OutputStream;
            return;
        }

        var rangeString = request.Headers.Get("Range");
        var rangeValue = RangeHeaderValue.Parse(rangeString);
        var range = rangeValue.Ranges.First();

        resp.StatusCode = (int)HttpStatusCode.PartialContent;
        resp.StatusDescription = "Partial Content";
        resp.ProtocolVersion = HttpVersion.Version11;
        resp.Headers.Add(HttpResponseHeader.ContentType, "application/octet-stream");
        resp.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
        resp.Headers.Add(HttpResponseHeader.KeepAlive, "true");
        resp.Headers.Add(HttpResponseHeader.ContentRange, range.ToString());
        await SendContent(resp, new MemoryStream(LargeData), range, truncate);

    }

    private async Task SendContent(HttpListenerResponse resp, Stream src, RangeItemHeaderValue range, bool truncate = false)
    {
        var from = range.From ?? 0;
        var to = range.To ?? src.Length;
        await using var ros = resp.OutputStream;
        src.Position = from;

        var count = to - from + 1;

        if (truncate && count > MB * 2)
            count = Random.Shared.Next(MB, MB * 2);

        var buffer = new byte[count];
        await src.ReadExactlyAsync(buffer);
        await ros.WriteAsync(buffer);
    }

    public Uri Uri => new(_prefix);

    private (HttpListener Listener, string Prefix) CreateNewListener()
    {
        HttpListener mListener;
        while (true)
        {
            mListener = new HttpListener();
            var newPort = Random.Shared.Next(49152, 65535);
            mListener.Prefixes.Add($"http://127.0.0.1:{newPort}/");
            try
            {
                mListener.Start();
            }
            catch
            {
                continue;
            }
            break;
        }

        return (mListener, mListener.Prefixes.First());
    }

    public void Dispose()
    {
        _listener.Stop();
    }
}
