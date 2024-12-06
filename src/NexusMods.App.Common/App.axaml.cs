using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.CLI.Types;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.Logging;

namespace NexusMods.App.Common;

public class App : Application
{
    private readonly IServiceProvider _provider;
    private readonly ISettingsManager _settingsManager;

    [UsedImplicitly]
    public App(IServiceProvider provider)
    {
        _provider = provider;
        _settingsManager = provider.GetRequiredService<ISettingsManager>();
        Name = "Nexus Mods";
        
    }

    public App()
    {
        throw new UnreachableException("We don't use the Runtime Loader, so this should never be called.");
    }

    
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var loggerFactory = _provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<App>();

        var uiCulture = _settingsManager.Get<LanguageSettings>().UICulture;
        logger.LogInformation("Using UI Culture {Culture}", uiCulture.Name);

        Thread.CurrentThread.CurrentUICulture = uiCulture;

        Locator.CurrentMutable.UnregisterCurrent(typeof(IViewLocator));
        Locator.CurrentMutable.Register(() => _provider.GetRequiredService<InjectedViewLocator>(), typeof(IViewLocator));

        Locator.CurrentMutable.UseMicrosoftExtensionsLoggingWithWrappingFullLogger(loggerFactory);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var reactiveWindow = _provider.GetRequiredService<MainWindow>();
            reactiveWindow.ViewModel = _provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = reactiveWindow;

            var activatableLifetime = TryGetFeature(typeof(IActivatableLifetime));
            if (activatableLifetime is IActivatableLifetime activatable)
            {
                activatable.Activated += async (_, args) =>
                {
                    switch (args.Kind)
                    {
                        case ActivationKind.OpenUri:
                            var protocol = (ProtocolActivatedEventArgs)args;
                            logger.LogInformation("Got Url {Url}", protocol.Uri.ToString());
                            var handler = _provider.GetRequiredService<IIpcProtocolHandler>();
                            await handler.Handle(protocol.Uri.ToString(), CancellationToken.None);
                            break;
                        default:
                            break;
                    }
                };
            }
            
        }

        base.OnFrameworkInitializationCompleted();
    }
}
