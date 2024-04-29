using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI;
using NexusMods.App.UI.Windows;
using NexusMods.SingleProcess;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using ReactiveUI;
using Splat;

namespace NexusMods.App;



// ReSharper disable once ClassNeverInstantiated.Global
public class Startup
{
    private static bool _hasBeenSetup = false;
    private static IServiceProvider _provider = null!;
    private static ILogger<Startup> _logger = null!;
    private static ulong _windowCount;

#pragma warning disable CS0028 // Disables warning about not being a valid entry point
    
    /// <summary>
    /// Main entry point for the application, the application loop will only exit when the token is cancelled.
    /// </summary>
    public static void Main(IServiceProvider provider, string[] args)
    {
        if (_hasBeenSetup)
            throw new InvalidOperationException("Startup has already been called!");
        
        _hasBeenSetup = true;
        _provider = provider;
        _logger = provider.GetRequiredService<ILogger<Startup>>();
        
        var logger = provider.GetRequiredService<ILogger<Startup>>();
        var builder = BuildAvaloniaApp(provider);

        // NOTE(erri120): DI is lazy by default and these services
        // do additional initialization inside their constructors.
        // We need to make sure their constructors are called to
        // finalize our OpenTelemetry configuration.
        provider.GetService<TracerProvider>();
        provider.GetService<MeterProvider>();

        try
        {
            // We aren't going to use the standard Avalonia startup because we don't want the app to exit
            // when the last window closes. Instead we want to exit only when the single process IPC server
            // has shutdown
            builder.Start(AppMain, args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Exception crashed the application!");
        }
    }
#pragma warning restore CS0028

    private static void AppMain(Application app, string[] args)
    {
        _logger.LogInformation("Starting UI application");
        ShowMainWindow();

        // Start the app, linking to the global shutdown token
        _logger.LogInformation("Starting application loop");
        app.Run(MainThreadData.GlobalShutdownToken);
        _logger.LogInformation("Application loop ended");
    }

    internal static void ShowMainWindow()
    {
        // TODO: enable multi-window support
        // https://github.com/Nexus-Mods/NexusMods.App/issues/1267
        if (Interlocked.Read(ref _windowCount) > 0)
        {
            _logger.LogError("We currently only allow 1 MainWindow. See https://github.com/Nexus-Mods/NexusMods.App/issues/1267 for details");
            return;
        }

        var reactiveWindow = _provider.GetRequiredService<MainWindow>();
        reactiveWindow.ViewModel = _provider.GetRequiredService<MainWindowViewModel>();
        reactiveWindow.WhenActivated(d =>
            {
                Interlocked.Increment(ref _windowCount);
                var token = _provider.GetRequiredService<MainProcessDirector>().MakeKeepAliveToken();
                _logger.LogDebug("MainWindow activated");
                token.DisposeWith(d);
                Disposable.Create(() =>
                    {
                        if (Interlocked.Decrement(ref _windowCount) == 0 && MainThreadData.IsDebugMode)
                        {
                            _logger.LogDebug("Final window closed, in debug mode, shutting down");
                            MainThreadData.Shutdown();
                        }
                        _logger.LogDebug("MainWindow deactivated");
                    }
                ).DisposeWith(d);
            }
        );
        reactiveWindow.Show();
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
    {
        ReactiveUiExtensions.DefaultLogger = serviceProvider.GetRequiredService<ILogger<Startup>>();

        IconProvider.Current
            .Register<MaterialDesignIconProvider>();

        var app = AppBuilder
            .Configure(serviceProvider.GetRequiredService<App>)
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();

        Locator.CurrentMutable.UnregisterCurrent(typeof(IViewLocator));
        Locator.CurrentMutable.Register(serviceProvider.GetRequiredService<InjectedViewLocator>, typeof(IViewLocator));

        return app;
    }
}
