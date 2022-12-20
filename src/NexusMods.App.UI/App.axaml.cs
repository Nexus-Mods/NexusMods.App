using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.ViewModels;
using NexusMods.App.UI.Views;

namespace NexusMods.App.UI;

public partial class App : Application
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
            desktop.MainWindow = _provider.GetRequiredService<MainWindow>();
            desktop.MainWindow.DataContext = _provider.GetRequiredService<MainWindowViewModel>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}