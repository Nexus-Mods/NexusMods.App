
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.ViewModels;
using NexusMods.App.UI.Views;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c)
    {
        c.AddTransient<MainWindow>();
        c.AddTransient<MainWindowViewModel>();
        c.AddSingleton<App>();
        return c;
    }
    
}