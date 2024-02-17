using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI;
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
    #pragma warning disable CS0028 // Disables warning about not being a valid entry point
    public static void Main(IServiceProvider provider, string[] args)
    {
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
            builder.StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Exception crashed the application!");
        }
    }
#pragma warning restore CS0028

    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
    {
        ReactiveUiExtensions.DefaultLogger = serviceProvider.GetRequiredService<ILogger<Startup>>();

        IconProvider.Current
            .Register<MaterialDesignIconProvider>();

        var app = AppBuilder.Configure(serviceProvider.GetRequiredService<App>)
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();

        Locator.CurrentMutable.UnregisterCurrent(typeof(IViewLocator));
        Locator.CurrentMutable.Register(serviceProvider.GetRequiredService<InjectedViewLocator>, typeof(IViewLocator));

        return app;
    }
}
