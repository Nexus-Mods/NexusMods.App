using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class LocalHttpServer : IDisposable
{
    private readonly ILogger<LocalHttpServer> _logger;
    private readonly HttpListener _listener;
    private readonly string _prefix;
    private readonly Task _loopTask;

    public LocalHttpServer(ILogger<LocalHttpServer> logger)
    {
        _logger = logger;
        (_listener, _prefix) = CreateNewListener();
        _listener.Start();
        _loopTask = StartLoop();
    }

    private Task StartLoop()
    {
        return Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                _logger.LogInformation("Got connection");
                using var resp = context.Response;

                switch (context.Request.Url.PathAndQuery)
                {
                    case "/hello":
                    {
                        resp.StatusCode = 200;
                        resp.StatusDescription = "OK";
                        await using var ros = resp.OutputStream;
                        ros.Write("Hello World!"u8);
                        break;
                    }
                    case "/100MB-Break":
                    {
                        resp.StatusCode = 200;
                        resp.StatusDescription = "OK";
                        await using var ros = resp.OutputStream;
                        ros.Write("Hello World!"u8);
                        break;
                    }
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