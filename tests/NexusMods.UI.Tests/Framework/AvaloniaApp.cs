using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NexusMods.App.UI;
using NexusMods.App.UI.Windows;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using ReactiveUI;

namespace NexusMods.UI.Tests.Framework;

public class AvaloniaApp : IDisposable
{
    private readonly IServiceProvider _provider;
    private bool _disposed;
    private readonly ILogger<AvaloniaApp> _logger;

    public AvaloniaApp(ILogger<AvaloniaApp> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
        Setup();
    }

    public void Stop()
    {
        var app = GetApp();
        if (app is IDisposable disposable)
        {
            Dispatcher.UIThread.Post(disposable.Dispose);
        }

        Dispatcher.UIThread.Post(() => app.Shutdown());
    }

    public static MainWindow GetMainWindow() => (MainWindow) GetApp().MainWindow;

    public static IClassicDesktopStyleApplicationLifetime GetApp() =>
        (IClassicDesktopStyleApplicationLifetime) Application.Current.ApplicationLifetime;

    private AppBuilder BuildAvaloniaApp()
    {
        return App.UI.Startup.BuildAvaloniaApp(_provider)
            .UseHeadless(new AvaloniaHeadlessPlatformOptions())
            .UseSkia();
    }

    private void Setup()
    {
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                BuildAvaloniaApp()
                    .SetupWithoutStarting();
                
                tcs.SetResult();
                Dispatcher.UIThread.MainLoop(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "While setting up AvaloniaApp");
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        tcs.Task.Wait();
    }

    public async Task<ControlHost<TView, TVm, TInterface>> GetControl<TView, TVm, TInterface>()
        where TView : ReactiveUserControl<TInterface>, new()
        where TInterface : class, IViewModelInterface
        where TVm : AViewModel<TInterface>, new()
    {
        var tcs = new TaskCompletionSource<bool>();
        var (waitHandle, host) = await Dispatcher.UIThread.InvokeAsync(() =>
        {

            var window = new Window();

            var control = new TView();
            var context = new TVm();
            control.DataContext = context;
            window.Content = control;
            window.Width = 1280;
            window.Height = 720;
            var waitHandle = context.Activator.Activated.Subscribe(_ => tcs.TrySetResult(true));
            window.Show();
            return (waitHandle, new ControlHost<TView, TVm, TInterface>
            {
                Window = window,
                View = control,
                ViewModel = context,
                App = this
            });
        });

        await tcs.Task;
        waitHandle.Dispose();
        
        return host;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();

    }
}
