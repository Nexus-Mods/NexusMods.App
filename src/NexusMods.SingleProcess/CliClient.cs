using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole;
using NexusMods.SingleProcess.Exceptions;
using Spectre.Console;

namespace NexusMods.SingleProcess;

/// <summary>
/// Client helper for connecting to a CliServer and executing commands
/// </summary>
public class CliClient(ILogger<CliClient> logger, SyncFile syncFile, IServiceProvider provider)
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    /// <summary>
    /// Starts the client process, connecting to the main process and running the console response loop
    /// </summary>
    public async Task ExecuteCommand(string[] args, IAnsiConsole? console = null)
    {
        var (process, port) = syncFile.GetSyncInfo();
        if (process is null)
        {
            throw new NoMainProcessStarted();
        }

        logger.LogInformation("Found main process {ProcessId} listening on port {Port}", process.Id, port);

        _client = new TcpClient();
        _client.NoDelay = true; // Disable Nagle's algorithm to reduce delay.
        try
        {
            await _client.ConnectAsync(IPAddress.Loopback, port);
            _stream = _client.GetStream();

            logger.LogDebug("Connected to main process {ProcessId} on port {Port}", process.Id, port);
            await RunTillCloseAsync(new ConsoleSettings
            {
                Arguments = args,
                Renderer = new SpectreRenderer(console ?? AnsiConsole.Console),
            });
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
}
