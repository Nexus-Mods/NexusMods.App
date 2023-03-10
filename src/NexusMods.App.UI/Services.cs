
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.Spine.Buttons;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.LeftMenu.Home;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.Home;
using NexusMods.App.UI.Windows;
using ImageButton = NexusMods.App.UI.Controls.Spine.Buttons.Image.ImageButton;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c)
    {
        return c.AddTransient<MainWindow>()
            // View Models
            .AddTransient<MainWindowViewModel>()

            .AddViewModel<FoundGamesViewModel, IFoundGamesViewModel>()
            .AddViewModel<GameWidgetViewModel, IGameWidgetViewModel>()
            .AddViewModel<IconButtonViewModel, IIconButtonViewModel>()
            .AddViewModel<ImageButtonViewModel, IImageButtonViewModel>()
            .AddViewModel<SpineViewModel, ISpineViewModel>()
            .AddViewModel<TopBarViewModel, ITopBarViewModel>()
            .AddViewModel<IconViewModel, IIconViewModel>()
            .AddViewModel<HomeLeftMenuViewModel, IHomeLeftMenuViewModel>()
            .AddViewModel<HomeViewDesignerViewModel, IHomeViewModel>()

            // Views
            .AddView<GameWidget, IGameWidgetViewModel>()
            .AddView<IconButton, IIconButtonViewModel>()
            .AddView<Spine, ISpineViewModel>()
            .AddView<FoundGamesView, IFoundGamesViewModel>()
            .AddView<ImageButton, IImageButtonViewModel>()
            .AddView<TopBarView, ITopBarViewModel>()
            .AddView<LeftMenuView, ILeftMenuViewModel>()
            .AddView<IconView, IIconViewModel>()
            .AddView<HomeLeftMenuView, IHomeLeftMenuViewModel>()
            .AddView<HomeView, IHomeViewModel>()

            // Other
            .AddSingleton<InjectedViewLocator>()
            .AddSingleton<App>();
    }

}
