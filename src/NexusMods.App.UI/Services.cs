
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.App.UI.LeftMenu.Game;
using NexusMods.App.UI.LeftMenu.Home;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.Downloads;
using NexusMods.App.UI.RightContent.Home;
using NexusMods.App.UI.RightContent.LoadoutGrid;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using NexusMods.App.UI.RightContent.MyGames;
using NexusMods.App.UI.Routing;
using NexusMods.App.UI.Windows;
using ReactiveUI;
using ImageButton = NexusMods.App.UI.Controls.Spine.Buttons.Image.ImageButton;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c)
    {
        return c.AddTransient<MainWindow>()

            // Services
            .AddSingleton<IRouter, ReactiveMessageRouter>()
            // View Models
            .AddTransient<MainWindowViewModel>()
            .AddTransient(typeof(DataGridViewModelColumn<,>))
            .AddTransient(typeof(DataGridColumnFactory<,>))
            .AddSingleton<IViewLocator, InjectedViewLocator>()

            .AddViewModel<CompletedViewModel, ICompletedViewModel>()
            .AddViewModel<DownloadsViewModel, IDownloadsViewModel>()
            .AddViewModel<FoundGamesViewModel, IFoundGamesViewModel>()
            .AddViewModel<GameLeftMenuViewModel, IGameLeftMenuViewModel>()
            .AddViewModel<GameWidgetViewModel, IGameWidgetViewModel>()
            .AddViewModel<HistoryViewModel, IHistoryViewModel>()
            .AddViewModel<HomeLeftMenuViewModel, IHomeLeftMenuViewModel>()
            .AddViewModel<HomeViewDesignerViewModel, IHomeViewModel>()
            .AddViewModel<IconButtonViewModel, IIconButtonViewModel>()
            .AddViewModel<IconViewModel, IIconViewModel>()
            .AddViewModel<ImageButtonViewModel, IImageButtonViewModel>()
            .AddViewModel<InProgressViewModel, IInProgressViewModel>()
            .AddViewModel<LaunchButtonViewModel, ILaunchButtonViewModel>()
            .AddViewModel<LoadoutGridViewModel, ILoadoutGridViewModel>()
            .AddViewModel<ModCategoryViewModel, IModCategoryViewModel>()
            .AddViewModel<ModEnabledViewModel, IModEnabledViewModel>()
            .AddViewModel<ModInstalledViewModel, IModInstalledViewModel>()
            .AddViewModel<ModNameViewModel, IModNameViewModel>()
            .AddViewModel<ModVersionViewModel, IModVersionViewModel>()
            .AddViewModel<MyGamesViewModel, IMyGamesViewModel>()
            .AddViewModel<NexusLoginOverlayViewModel, INexusLoginOverlayViewModel>()
            .AddViewModel<PlaceholderDesignViewModel, IPlaceholderViewModel>()
            .AddViewModel<SpineViewModel, ISpineViewModel>()
            .AddViewModel<TopBarViewModel, ITopBarViewModel>()
            .AddViewModel<DownloadButtonViewModel, IDownloadButtonViewModel>()

            // Views
            .AddView<CompletedView, ICompletedViewModel>()
            .AddView<DownloadsView, IDownloadsViewModel>()
            .AddView<FoundGamesView, IFoundGamesViewModel>()
            .AddView<GameLeftMenuView, IGameLeftMenuViewModel>()
            .AddView<GameWidget, IGameWidgetViewModel>()
            .AddView<HistoryView, IHistoryViewModel>()
            .AddView<HomeLeftMenuView, IHomeLeftMenuViewModel>()
            .AddView<HomeView, IHomeViewModel>()
            .AddView<IconButton, IIconButtonViewModel>()
            .AddView<IconView, IIconViewModel>()
            .AddView<ImageButton, IImageButtonViewModel>()
            .AddView<InProgressView, IInProgressViewModel>()
            .AddView<LaunchButtonView, ILaunchButtonViewModel>()
            .AddView<LeftMenuView, ILeftMenuViewModel>()
            .AddView<LoadoutGridView, ILoadoutGridViewModel>()
            .AddView<ModCategoryView, IModCategoryViewModel>()
            .AddView<ModEnabledView, IModEnabledViewModel>()
            .AddView<ModInstalledView, IModInstalledViewModel>()
            .AddView<ModNameView, IModNameViewModel>()
            .AddView<ModVersionView, IModVersionViewModel>()
            .AddView<MyGamesView, IMyGamesViewModel>()
            .AddView<NexusLoginOverlayView, INexusLoginOverlayViewModel>()
            .AddView<PlaceholderView, IPlaceholderViewModel>()
            .AddView<Spine, ISpineViewModel>()
            .AddView<TopBarView, ITopBarViewModel>()
            .AddView<DownloadButtonView, IDownloadButtonViewModel>()

            // Other
            .AddSingleton<InjectedViewLocator>()
            .AddSingleton<App>();
    }

}
