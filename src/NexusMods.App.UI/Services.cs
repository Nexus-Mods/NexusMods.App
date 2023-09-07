
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
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
using NexusMods.App.UI.Overlays.Download.Cancel;
using NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadGameName;
using NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadStatus;
using NexusMods.App.UI.RightContent.Downloads;
using NexusMods.App.UI.RightContent.Home;
using NexusMods.App.UI.RightContent.LoadoutGrid;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadSize;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadVersion;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModInstalled;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModName;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModVersion;
using NexusMods.App.UI.RightContent.MyGames;
using NexusMods.App.UI.Routing;
using NexusMods.App.UI.Windows;
using NexusMods.Common;
using ReactiveUI;
using DownloadGameNameView = NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadGameName.DownloadGameNameView;
using DownloadNameView = NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName.DownloadNameView;
using DownloadStatusView = NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadStatus.DownloadStatusView;
using ImageButton = NexusMods.App.UI.Controls.Spine.Buttons.Image.ImageButton;
using ModCategoryView = NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory.ModCategoryView;
using ModEnabledView = NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModEnabled.ModEnabledView;
using ModInstalledView = NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModInstalled.ModInstalledView;
using ModNameView = NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModName.ModNameView;
using ModVersionView = NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModVersion.ModVersionView;
using NexusLoginOverlayView = NexusMods.App.UI.Overlays.Login.NexusLoginOverlayView;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c, ILauncherSettings? settings)
    {
        if (settings == null)
            c.AddSingleton<ILauncherSettings, LauncherSettings>();
        else
            c.AddSingleton(settings);

        return c.AddTransient<MainWindow>()

            // Services
            .AddSingleton<IRouter, ReactiveMessageRouter>()
            .AddSingleton<IOverlayController, OverlayController>()

            // View Models
            .AddTransient<MainWindowViewModel>()
            .AddTransient(typeof(DataGridViewModelColumn<,>))
            .AddTransient(typeof(DataGridColumnFactory<,,>))
            .AddSingleton<IViewLocator, InjectedViewLocator>()

            .AddViewModel<CompletedViewModel, ICompletedViewModel>()
            .AddViewModel<DevelopmentBuildBannerViewModel, IDevelopmentBuildBannerViewModel>()
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
            .AddViewModel<DownloadGameNameViewModel, IDownloadGameNameViewModel>()
            .AddViewModel<DownloadNameViewModel, IDownloadNameViewModel>()
            .AddViewModel<DownloadVersionViewModel, IDownloadVersionViewModel>()
            .AddViewModel<DownloadSizeViewModel, IDownloadSizeViewModel>()
            .AddViewModel<DownloadStatusViewModel, IDownloadStatusViewModel>()
            .AddViewModel<CancelDownloadOverlayViewModel, ICancelDownloadOverlayViewModel>()
            .AddViewModel<MessageBoxOkCancelViewModel, IMessageBoxOkCancelViewModel>()
            .AddViewModel<MetricsOptInViewModel, IMetricsOptInViewModel>()

            // Views
            .AddView<CompletedView, ICompletedViewModel>()
            .AddView<DevelopmentBuildBannerView, IDevelopmentBuildBannerViewModel>()
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
            .AddView<MetricsOptInView, IMetricsOptInViewModel>()
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
            .AddView<DownloadGameNameView, IDownloadGameNameViewModel>()
            .AddView<DownloadNameView, IDownloadNameViewModel>()
            .AddView<DownloadVersionView, IDownloadVersionViewModel>()
            .AddView<DownloadSizeView, IDownloadSizeViewModel>()
            .AddView<DownloadStatusView, IDownloadStatusViewModel>()
            .AddView<CancelDownloadOverlayView, ICancelDownloadOverlayViewModel>()
            .AddView<MessageBoxOkCancelView, IMessageBoxOkCancelViewModel>()

            // Other
            .AddSingleton<InjectedViewLocator>()
            .AddSingleton<App>();
    }

}
