using Avalonia;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Windows;

namespace NexusMods.UI.Tests.Framework;

public class WindowHost : IAsyncDisposable
{
    private readonly MainWindow _window;
    private readonly MainWindowViewModel _viewModel;
    private readonly ILogger<WindowHost> _logger;

    public MainWindow Window => _window;
    public MainWindowViewModel ViewModel => _viewModel;

    public WindowHost(MainWindow window, MainWindowViewModel viewModel, ILogger<WindowHost> logger)
    {
        _window = window;
        _viewModel = viewModel;
        _logger = logger;

    }

    public async ValueTask DisposeAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // If we close here, the app hangs when using the headless renderer.
            // so either we can use the platform renderer (and have flashing windows during testing)
            // or we hide the window instead :|
            _window.Hide();
        });
    }

    /// <summary>
    /// Executes an action on the UI thread and waits for it to complete.
    /// </summary>
    /// <param name="action"></param>
    public static async Task OnUi(Func<Task> action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
        await Flush();
    }

    public static async Task<T> OnUi<T>(Func<Task<T>> action)
    {
        var res = await Dispatcher.UIThread.InvokeAsync(action);
        await Flush();
        return res;
    }

    /// <summary>
    /// Insures that all pending UI actions have been completed.
    /// </summary>
    public static async Task Flush()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }

    /// <summary>
    /// Use Avalonia selectors to find controls in the current Window. For now the only
    /// types supported are those found in the Avalonia.* namespace.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="maxTries"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<IEnumerable<T>> Select<T>(string name, int maxTries = 5) where T : StyledElement
    {
        var timeout = Environment.GetEnvironmentVariable("GITHUB_CI") is not null
            ? TimeSpan.FromMilliseconds(500)
            : TimeSpan.FromMilliseconds(100);

        var tries = 0;
        while (tries <= maxTries)
        {
            tries++;

            var tmp = await OnUi(() =>
            {
                return Task.FromResult(_window.GetVisualDescendants()
                    .OfType<StyledElement>()
                    .Where(x => x.Name == name)
                    .OfType<T>()
                    .ToArray());
            });

            if (tmp.Any())
            {
                _logger.LogInformation("Rendering finished after {Count} try/tries", tries);
                return tmp;
            }

            await Task.Delay(timeout);
        }

        _logger.LogWarning("Unable to select elements, even after {Count} tries", tries);
        return Array.Empty<T>();
    }
}
