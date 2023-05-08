using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

/// <summary>
/// A base test class for a test that needs the entire app, the MainWindow and a full View Model
/// </summary>
public class AEndToEndTest : IAsyncLifetime
{
    private readonly AvaloniaApp _app;

    protected WindowHost Host => _host!;
    private WindowHost? _host;

    protected AEndToEndTest(AvaloniaApp app)
    {
        _app = app;

    }

    public async Task InitializeAsync()
    {
        _host = await _app.GetMainWindow();
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
    }
}
