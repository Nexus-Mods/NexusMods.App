using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Parsers;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NexusMods.App.UI.Windows;
using NexusMods.Paths;

namespace NexusMods.UI.Tests.Framework;

public class WindowHost : IAsyncDisposable
{
    private readonly MainWindow _window;
    private readonly MainWindowViewModel _viewModel;
    private readonly SelectorParser _parser;

    public WindowHost(MainWindow window, MainWindowViewModel viewModel)
    {
        _window = window;
        _viewModel = viewModel;
        var types = new Lazy<Dictionary<string, Type>>(GetTypes);
        _parser = new SelectorParser((_, name) => types.Value[name]);
    }

    private Dictionary<string,Type> GetTypes()
    {
        var types = new Dictionary<string, Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
                continue;
            if (!assembly.FullName!.StartsWith("Avalonia."))
                continue;
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(Control)))
                {
                    types.Add(type.Name, type);
                }
            }
        }

        return types;
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
    public async Task OnUi(Func<Task> action)
    {
        await Dispatcher.UIThread.InvokeAsync(action);
        await Flush();
    }

    /// <summary>
    /// Insures that all pending UI actions have been completed.
    /// </summary>
    public async Task Flush()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }

    /// <summary>
    /// Use Avalonia selectors to find controls in the current Window. For now the only
    /// types supported are those found in the Avalonia.* namespace.
    /// </summary>
    /// <param name="selector"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<IEnumerable<T>> Select<T>(string selector) where T : IStyleable
    {
        var parsed = _parser.Parse(selector);
        if (parsed == null)
            throw new ArgumentException("Invalid selector", nameof(selector));
        
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            return _window.GetVisualDescendants()
                .OfType<StyledElement>()
                .Where(x => parsed.Match(x).IsMatch)
                .OfType<T>()
                .ToArray();
        });
    }

    /// <summary>
    /// Take a snapshot of the current window state and save it to the screenshots folder which is located in the entry directory.
    /// </summary>
    /// <param name="name">Injected by the compiler, the name of the calling method</param>
    /// <param name="line">Injected by the compiler, the line of the calling method's invocation</param>
    /// <param name="wait">Wait for one second, while forcing a UI refresh every 100ms</param>
    public async Task SnapShot([CallerMemberName] string name = "", [CallerLineNumber] int line = 0, bool wait = true)
    {
        if (wait)
        {
            // 10 fps refresh for one sec to allow the UI to settle
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(100);
                AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
            }
        }

        using var frame = (_window.PlatformImpl as IHeadlessWindow)?.GetLastRenderedFrame();
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked("screenshots")
            .CombineUnchecked($"{name}_{line}.png");
        path.Parent.CreateDirectory();
        frame?.Item.Save(path.ToString());
    }
}
 