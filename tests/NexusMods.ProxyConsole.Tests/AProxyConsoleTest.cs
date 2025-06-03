using Nerdbank.Streams;
using NexusMods.Sdk.ProxyConsole;
using Xunit.Sdk;

namespace NexusMods.ProxyConsole.Tests;

public class AProxyConsoleTest : IAsyncLifetime
{
    private Stream _clientStream;
    private Stream _serverStream;
    protected readonly LoggingRenderer LoggingRenderer = new();
    private ClientRendererAdaptor? _client;
    protected IRenderer Server => _server!;
    private IRenderer? _server;
    private readonly IServiceProvider _provider;

    protected AProxyConsoleTest(IServiceProvider provider)
    {
        _provider = provider;
        (_serverStream, _clientStream) = FullDuplexStream.CreatePair();
    }

    public async Task InitializeAsync()
    {
        _client = new ClientRendererAdaptor(_clientStream, LoggingRenderer, _provider);
        (_, _server) = await ProxiedRenderer.Create(_provider, _serverStream);
    }

    public async Task EventuallyAsync(Func<Task> fn)
    {
        const int retries = 100;
        int attempts = 0;
        while (attempts++ < retries)
        {
            try
            {
                await fn();
                return;
            }
            catch (XunitException)
            {
                await Task.Delay(200).ConfigureAwait(false);
            }
        }
    }

    public async Task Eventually(Action fn)
    {
        await Task.Run(async () =>
        {
            const int retries = 10;
            var attempts = 0;
            Exception lastException = new("No exception thrown");
            while (attempts++ < retries)
            {
                try
                {
                    fn();
                    return;
                }
                catch (XunitException ex)
                {
                    Console.WriteLine("Wait");
                    await Task.Delay(200);
                    lastException = ex;
                }
            }

            throw lastException;
        });
    }

    public Task DisposeAsync()
    {
        if (_client != null && _client is IDisposable d)
        {
            d.Dispose();
        }

        if (_server != null && _server is IDisposable d2)
        {
            d2.Dispose();
        }

        return Task.CompletedTask;
    }
}
