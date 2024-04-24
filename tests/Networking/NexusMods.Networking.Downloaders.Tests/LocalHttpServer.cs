using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NexusMods.Networking.Downloaders.Tests;

public class LocalHttpServer : IDisposable
{
    private string _prefix;
    private HttpListener _listener;
    private readonly ILogger _logger;
    private ConcurrentDictionary<string, byte[]> _content = new();

    public LocalHttpServer(ILogger<LocalHttpServer> logger)
    {
        _logger = logger;
        (_listener, _prefix) = CreateNewListener();
        StartLoop();
    }

    public void SetContent(string path, byte[] content)
    {
        _content[$"/{path}"] = content;
    }

    private void StartLoop()
    {
        Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                _logger.LogInformation("Got connection");

                while (IsPaused)
                {
                    _logger.LogDebug("Server is paused, waiting for 100ms");
                    await Task.Delay(100);
                }
                
                using var resp = context.Response;
                
                var responseData = _content[context.Request.Url!.LocalPath];
                resp.StatusCode = 200;
                resp.StatusDescription = "OK";
                resp.ProtocolVersion = HttpVersion.Version11;
                resp.ContentLength64 = responseData.Length;
                if (context.Request.HttpMethod != "HEAD")
                {
                    await using var ros = resp.OutputStream;
                    await ros.WriteAsync(responseData);
                }
            }
        });
    }
    
    public bool IsPaused { get; set; }
    
    public Uri Prefix => new(_prefix);
    
    
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
