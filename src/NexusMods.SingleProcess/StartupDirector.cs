using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions;
using Spectre.Console;

namespace NexusMods.SingleProcess;

/// <summary>
/// A class that will either create a new UI instance or call the main process passing in CLI arguments,
/// depending on the state of the system.
/// </summary>
public class StartupDirector
{
    private readonly ILogger<StartupDirector> _logger;
    private readonly SingleProcessSettings _settings;
    private readonly IStartupHandler _handler;
    private readonly IServiceProvider _provider;
    private MainProcessDirector? _mainProcessDirector;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="settings"></param>
    /// <param name="startupHandler"></param>
    /// <param name="provider"></param>
    public StartupDirector(ILogger<StartupDirector> logger, SingleProcessSettings settings, IStartupHandler startupHandler, IServiceProvider provider)
    {
        _logger = logger;
        _settings = settings;
        _handler = startupHandler;
        _provider = provider;
    }

    /// <summary>
    /// Starts the application. If arguments are passed this will attempt to start the main process, and funnel them to it,
    /// unless the main process is already running, in which case it will forward the command to the existing main process.
    /// If debug mode is enabled, the main process will never be started, and the code will run directly in this process,
    /// to allow for easier use with a debugger.
    ///
    /// If a IAnsiConsole is passed in, it will be used to render the CLI commands, otherwise AnsiConsole.Console will be used
    /// </summary>
    /// <param name="args"></param>
    /// <param name="console"></param>
    /// <returns></returns>
    public async Task<int> Start(string[] args, IAnsiConsole? console = null)
    {
        _logger.LogInformation("Starting application with args: {Arguments}", string.Join(' ', args));
        if (args.Length == 0)
        {
            return await BecomeMainProcess();
        }
        else
        {
            return await SendCommandAsync(args, console);
        }
    }

    /// <summary>
    /// Connects to the main process and sends the CLI arguments to it.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="console"></param>
    /// <returns></returns>
    private async Task<int> SendCommandAsync(string[] args, IAnsiConsole? console)
    {
        var sw = Stopwatch.StartNew();
        Exception? lastException = null;

        var client = _provider.GetRequiredService<ClientProcessDirector>();
        if (!client.IsMainProcessRunning)
            await _handler.StartMainProcess();

        while (sw.Elapsed < _settings.ClientConnectTimeout)
        {
            try
            {
                await client.StartClientAsync(new ConsoleSettings
                {
                    Arguments = args,
                    Renderer = new SpectreRenderer(console ?? AnsiConsole.Console)
                });
                return 0;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(10);
            }
        }
        _logger.LogError(lastException, "Failed to connect to main process");
        return -1;
    }

    /// <summary>
    /// Starts the main process director locally
    /// </summary>
    /// <returns></returns>
    private async Task<int> BecomeMainProcess()
    {
        _mainProcessDirector = _provider.GetRequiredService<MainProcessDirector>();
        if (!await _mainProcessDirector.TryStartMainAsync(new Handler(this)))
        {
            _logger.LogError("Failed to start main process, trying to connect");
            return -1;
        }
        while (_mainProcessDirector.IsListening)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        return 0;
    }

    private class Handler(StartupDirector director) : IMainProcessHandler
    {
        public async Task HandleAsync(string[] arguments, IRenderer console, CancellationToken token)
        {
            await director._handler.HandleCliCommandAsync(arguments, console, token);
        }
    }



}
