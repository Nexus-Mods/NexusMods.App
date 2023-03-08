using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Windows;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.Logging;

namespace NexusMods.App.UI;

public class App : Application
{
    private readonly IServiceProvider _provider;

    public App(IServiceProvider provider)
    {
        _provider = provider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Locator.CurrentMutable.UnregisterCurrent(typeof(IViewLocator));
            Locator.CurrentMutable.Register(() => _provider.GetRequiredService<InjectedViewLocator>(), typeof(IViewLocator));

            var loggerFactory = _provider.GetRequiredService<ILoggerFactory>();
            Locator.CurrentMutable.UseMicrosoftExtensionsLoggingWithWrappingFullLogger(loggerFactory);


            var reactiveWindow = _provider.GetRequiredService<MainWindow>();
            reactiveWindow.ViewModel = _provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = reactiveWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
