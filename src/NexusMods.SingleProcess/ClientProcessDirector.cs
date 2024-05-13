using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole;

namespace NexusMods.SingleProcess;

/// <summary>
/// A director that will connect to the main process and connect the console to it
/// </summary>
public class ClientProcessDirector(ILogger<ClientProcessDirector> logger, SingleProcessSettings settings, IServiceProvider provider)
    : ADirector(settings)
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    /// <summary>
    /// True if the main process is running
    /// </summary>
    public bool IsMainProcessRunning
    {
        get
        {
            ConnectSharedArray();
            var (process, _) = GetSyncInfo();
            return process is not null;
        }
    }

    /// <summary>
    /// Starts the client process, connecting to the main process and running the console response loop
    /// </summary>
    /// <param name="proxy"></param>
    /// <exception cref="InvalidDataException"></exception>
    /// <exception cref="SocketException"></exception>
    public async Task StartClientAsync(ConsoleSettings proxy)
    {
        ConnectSharedArray();

        var (process, port) = GetSyncInfo();
        if (process is null)
        {
            throw new InvalidDataException("No main process is started");
        }

        logger.LogInformation("Found main process {ProcessId} listening on port {Port}", process.Id, port);

        _client = new TcpClient();
        _client.NoDelay = true; // Disable Nagle's algorithm to reduce delay.
        try
        {
            await _client.ConnectAsync(IPAddress.Loopback, port);
            _stream = _client.GetStream();

            logger.LogDebug("Connected to main process {ProcessId} on port {Port}", process.Id, port);
            await RunTillCloseAsync(proxy);
        }
        catch (SocketException)
        {
            logger.LogWarning("Failed to connect to main process {ProcessId} on port {Port}", process.Id, port);
            throw;
        }
    }

    private async Task RunTillCloseAsync(ConsoleSettings proxy)
    {
        try
        {
            var adaptor = new ClientRendererAdaptor(_stream!, proxy.Renderer, provider, proxy.Arguments);
            await adaptor.RunningTask;
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "Client disconnected");
        }
    }

    /// <summary>
    /// Async dispose
    /// </summary>
    public override ValueTask DisposeAsync()
    {
        _client?.Dispose();
        SharedArray?.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ClientProcessDirector"/>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static ClientProcessDirector Create(IServiceProvider serviceProvider)
    {
        return new ClientProcessDirector(serviceProvider.GetRequiredService<ILogger<ClientProcessDirector>>(),
            serviceProvider.GetRequiredService<SingleProcessSettings>(),
            serviceProvider);
    }
}
