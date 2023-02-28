using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using ReactiveUI;
using Splat;

namespace NexusMods.App.UI;

// ReSharper disable once ClassNeverInstantiated.Global
public class Startup
{
    public static void Main(IServiceProvider provider, string[] args) => BuildAvaloniaApp(provider)
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
    {
        return AppBuilder.Configure(serviceProvider.GetRequiredService<App>)
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI()
            .WithIcons(c => c.Register<MaterialDesignIconProvider>());
    }
}
