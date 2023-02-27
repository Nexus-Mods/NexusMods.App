
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.ViewModels;
using NexusMods.App.UI.Views;
using NexusMods.UI.Theme.Controls.Spine.Buttons;
using ReactiveUI;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c)
    {
        return c.AddTransient<MainWindow>()
            .AddTransient<MainWindowViewModel>()
            .AddTransient<SpineViewModel>()
            .AddTransient<HomeButtonViewModel>()
            .AddTransient<AddButtonViewModel>()
            .AddTransient<IViewFor<HomeButtonViewModel>, Home>()
            .AddTransient<IViewFor<AddButtonViewModel>, Add>()
            .AddTransient<FoundGamesViewModel>()
            .AddSingleton<InjectedViewLocator>()
            .AddTransient<IViewFor<SpineViewModel>, Spine>()
            .AddSingleton<App>();
    }
    
}