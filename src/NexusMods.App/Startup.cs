using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using ReactiveUI;
using Splat;

namespace NexusMods.App;



// ReSharper disable once ClassNeverInstantiated.Global
public class Startup
{
    #pragma warning disable CS0028 // Disables warning about not being a valid entry point
    public static void Main(IServiceProvider provider, string[] args) => BuildAvaloniaApp(provider)
        .StartWithClassicDesktopLifetime(args);
    #pragma warning restore CS0028 //

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
