using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.Hashing.xxHash64;

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
        var data = new byte[8 * 1024 * 1024];
        var seed = Random.Shared.Next(0, 255);
        for (var offset = 0; offset < data.Length; offset++)
            //data[offset] = (byte)((offset % 255) ^ seed);
            data[offset] = (byte)(offset % 255);

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
                    case "/100MB-Break":
                        {
                            resp.StatusCode = 200;
                            resp.StatusDescription = "OK";
                            resp.ProtocolVersion = HttpVersion.Version11;
                            resp.Headers.Add(HttpResponseHeader.ContentType, "text/plain");
                            await using var ros = resp.OutputStream;
                            ros.Write("Hello World!"u8);
                            break;
                        }
                    case "/resume":
                        {
                            resp.StatusCode = (int)HttpStatusCode.PartialContent;
                            resp.StatusDescription = "Partial Content";
                            resp.ProtocolVersion = HttpVersion.Version11;
                            resp.Headers.Add(HttpResponseHeader.ContentType, "text/plain");
                            // resp.Headers.Add(HttpResponseHeader.ContentType, "application/octet-stream");
                            resp.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
                            resp.Headers.Add(HttpResponseHeader.ContentRange, $"bytes 15-20/21");
                            resp.Headers.Add(HttpResponseHeader.KeepAlive, "true");

                            await using var ros = resp.OutputStream;
                            ros.Write("World!"u8);
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
        });
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

        var from = range.From ?? 0;
        var to = range.To == null ? LargeData.Length : range.To + 1;

        var originalSegment = LargeData[(int)from..(int)to];

        if (truncate && originalSegment.Length > MB * 2)
        {
            originalSegment = originalSegment[..Random.Shared.Next(MB, MB * 2)];
        }

        resp.StatusCode = (int)HttpStatusCode.PartialContent;
        resp.StatusDescription = "Partial Content";
        resp.ProtocolVersion = HttpVersion.Version11;
        resp.Headers.Add(HttpResponseHeader.ContentType, "application/octet-stream");
        resp.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
        resp.Headers.Add(HttpResponseHeader.KeepAlive, "true");
        resp.Headers.Add(HttpResponseHeader.ContentRange, range.ToString());
        await using var ros = resp.OutputStream;
        await ros.WriteAsync(originalSegment);

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
