
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.Spine.Buttons;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.ViewModels;
using NexusMods.App.UI.Views;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c)
    {
        return c.AddTransient<MainWindow>()
            // View Models
            .AddTransient<MainWindowViewModel>()
            .AddTransient<SpineViewModel>()
            .AddTransient<HomeButtonViewModel>()
            .AddTransient<AddButtonViewModel>()
            .AddTransient<FoundGamesViewModel>()
            .AddTransient<TopBarViewModel>()
            
            // Views
            .AddView<Home, HomeButtonViewModel>()
            .AddView<Add, AddButtonViewModel>()
            .AddView<Spine, SpineViewModel>()
            .AddView<FoundGamesView, FoundGamesViewModel>()
            .AddView<Game, GameViewModel>()
            .AddView<TopBarView, TopBarViewModel>()
            
            // Other
            .AddSingleton<InjectedViewLocator>()
            .AddSingleton<App>();
    }
    
}