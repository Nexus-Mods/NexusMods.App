using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI;
using NexusMods.App.UI.Windows;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.Logging;

namespace NexusMods.App;

public class App : Application
{
    private readonly IServiceProvider _provider;
    private readonly ILauncherSettings _launcherSettings;

    public App(IServiceProvider provider, ILauncherSettings launcherSettings)
    {
        _provider = provider;
        _launcherSettings = launcherSettings;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!string.IsNullOrEmpty(_launcherSettings.LocaleOverride))
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(_launcherSettings.LocaleOverride);

        Locator.CurrentMutable.UnregisterCurrent(typeof(IViewLocator));
        Locator.CurrentMutable.Register(() => _provider.GetRequiredService<InjectedViewLocator>(), typeof(IViewLocator));

        var loggerFactory = _provider.GetRequiredService<ILoggerFactory>();
        Locator.CurrentMutable.UseMicrosoftExtensionsLoggingWithWrappingFullLogger(loggerFactory);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var reactiveWindow = _provider.GetRequiredService<MainWindow>();
            reactiveWindow.ViewModel = _provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = reactiveWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
