using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Windows;

namespace NexusMods.UI.Tests.Framework;

/// <summary>
/// Takes care of lifecycle of the Avalonia app in a test state
/// </summary>
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

    private void Stop()
    {
        var app = GetApp();
        if (app is IDisposable disposable)
        {
            Dispatcher.UIThread.Post(disposable.Dispose);
        }

        Dispatcher.UIThread.Post(() => app?.Shutdown());
    }

    private static IClassicDesktopStyleApplicationLifetime? GetApp() =>
        (IClassicDesktopStyleApplicationLifetime?) Application.Current?.ApplicationLifetime;

    private AppBuilder BuildAvaloniaApp()
    {
        return App.Startup.BuildAvaloniaApp(_provider)
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

                // Keep this here to make sure we at least create the dispatcher before
                // reporting that we have started
                
                Dispatcher.UIThread.CheckAccess();
                
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

    /// <summary>
    /// Returns a control host that can be used to interact with the control. The VM
    /// in this case is the design time VM, so it will not be connected to any services
    /// this way the control can be tested in isolation.
    /// </summary>
    /// <typeparam name="TView">The view to construct</typeparam>
    /// <typeparam name="TVm">The VM instance to use when constructing the view (should be the Design variant)</typeparam>
    /// <typeparam name="TInterface">The VM interface expected by the view and view model</typeparam>
    /// <returns></returns>
    public async Task<ControlHost<TView, TVm, TInterface>> GetControl<TView, TVm, TInterface>()
        where TView : ReactiveUserControl<TInterface>, new()
        where TInterface : class, IViewModelInterface
        where TVm : TInterface, new()
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

    /// <summary>
    /// Constructs a window host that can be used to interact with the window, this will be a fully functional
    /// version of the app with all services connected and using a normal MainWindowViewModel
    /// </summary>
    /// <returns></returns>
    public async Task<WindowHost> GetMainWindow()
    {
        var tcs = new TaskCompletionSource<bool>();
        var (waitHandle, host) = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var window = new MainWindow();
            var context = _provider.GetRequiredService<MainWindowViewModel>();
            var logger = _provider.GetRequiredService<ILogger<WindowHost>>();

            window.DataContext = context;
            window.Width = 1280;
            window.Height = 720;

            var waitHandle = context.Activator.Activated.Subscribe(_ => tcs.TrySetResult(true));
            window.Show();

            return (waitHandle, new WindowHost(window, context, logger));
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
