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

public class App(IServiceProvider provider, ILauncherSettings launcherSettings)
    : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!string.IsNullOrEmpty(launcherSettings.LocaleOverride))
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(launcherSettings.LocaleOverride);

        Locator.CurrentMutable.UnregisterCurrent(typeof(IViewLocator));
        Locator.CurrentMutable.Register(() => provider.GetRequiredService<InjectedViewLocator>(), typeof(IViewLocator));

        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        Locator.CurrentMutable.UseMicrosoftExtensionsLoggingWithWrappingFullLogger(loggerFactory);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var reactiveWindow = provider.GetRequiredService<MainWindow>();
            reactiveWindow.ViewModel = provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = reactiveWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
