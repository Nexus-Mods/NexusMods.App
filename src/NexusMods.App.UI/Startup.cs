using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.App.UI;

public class Startup
{
    public static void Main(IServiceProvider provider, string[] args) => BuildAvaloniaApp(provider)
        .StartWithClassicDesktopLifetime(args);
    
    public static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
        => AppBuilder.Configure<App>(serviceProvider.GetRequiredService<App>)
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}
