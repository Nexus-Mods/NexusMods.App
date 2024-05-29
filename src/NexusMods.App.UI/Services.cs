using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Serialization.Json;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadGameName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadStatus;
using NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadVersion;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.ModInfo.Error;
using NexusMods.App.UI.Controls.ModInfo.Loading;
using NexusMods.App.UI.Controls.ModInfo.ModFiles;
using NexusMods.App.UI.Controls.Settings.SettingEntries;
using NexusMods.App.UI.Controls.Spine;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;
using NexusMods.App.UI.Controls.Spine.Buttons.Icon;
using NexusMods.App.UI.Controls.Spine.Buttons.Image;
using NexusMods.App.UI.Controls.TopBar;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Controls.Trees.Files;
using NexusMods.App.UI.DiagnosticSystem;
using NexusMods.App.UI.LeftMenu;
using NexusMods.App.UI.LeftMenu.Downloads;
using NexusMods.App.UI.LeftMenu.Home;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.LeftMenu.Loadout;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Overlays.Download.Cancel;
using NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.App.UI.Pages.Changelog;
using NexusMods.App.UI.Pages.Diagnostics;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Pages.Downloads;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName;
using NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion;
using NexusMods.App.UI.Pages.ModInfo;
using NexusMods.App.UI.Pages.ModLibrary;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceAttachments;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI;
using DownloadGameNameView = NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadGameName.DownloadGameNameView;
using DownloadNameView = NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName.DownloadNameView;
using DownloadSizeView = NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize.DownloadSizeView;
using DownloadStatusView = NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadStatus.DownloadStatusView;
using DownloadVersionView = NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadVersion.DownloadVersionView;
using ImageButton = NexusMods.App.UI.Controls.Spine.Buttons.Image.ImageButton;
using LoadingView = NexusMods.App.UI.Controls.ModInfo.Loading.LoadingView;
using ModCategoryView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory.ModCategoryView;
using ModEnabledView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModEnabled.ModEnabledView;
using ModInstalledView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModInstalled.ModInstalledView;
using ModNameView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModName.ModNameView;
using ModVersionView = NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModVersion.ModVersionView;
using NexusLoginOverlayView = NexusMods.App.UI.Overlays.Login.NexusLoginOverlayView;
using SettingToggleControl = NexusMods.App.UI.Controls.Settings.SettingEntries.SettingToggleControl;

namespace NexusMods.App.UI;

public static class Services
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddUI(this IServiceCollection c)
    {
        return c
            // JSON converters
            .AddSingleton<JsonConverter, RectJsonConverter>()
            .AddSingleton<JsonConverter, ColorJsonConverter>()
            .AddSingleton<JsonConverter, AbstractClassConverterFactory<IPageFactoryContext>>()
            .AddSingleton<JsonConverter, AbstractClassConverterFactory<IWorkspaceContext>>()

            // Type Finder
            .AddSingleton<ITypeFinder, TypeFinder>()

            .AddTransient<MainWindow>()

            // Services
            .AddSingleton<IOverlayController, OverlayController>()
            .AddTransient<IImageCache, ImageCache>()

            // View Models
            .AddTransient<MainWindowViewModel>()
            .AddTransient(typeof(DataGridViewModelColumn<,>))
            .AddTransient(typeof(DataGridColumnFactory<,,>))
            .AddSingleton<IViewLocator, InjectedViewLocator>()

            .AddViewModel<DevelopmentBuildBannerViewModel, IDevelopmentBuildBannerViewModel>()
            .AddViewModel<DownloadsLeftMenuViewModel, IDownloadsLeftMenuViewModel>()
            .AddViewModel<GameWidgetViewModel, IGameWidgetViewModel>()
            .AddViewModel<HomeLeftMenuViewModel, IHomeLeftMenuViewModel>()
            .AddViewModel<IconButtonViewModel, IIconButtonViewModel>()
            .AddViewModel<IconViewModel, IIconViewModel>()
            .AddViewModel<ImageButtonViewModel, IImageButtonViewModel>()
            .AddViewModel<InProgressViewModel, IInProgressViewModel>()
            .AddViewModel<LaunchButtonViewModel, ILaunchButtonViewModel>()
            .AddViewModel<ApplyControlViewModel, IApplyControlViewModel>()
            .AddViewModel<LoadoutGridViewModel, ILoadoutGridViewModel>()
            .AddViewModel<ModCategoryViewModel, IModCategoryViewModel>()
            .AddViewModel<ModEnabledViewModel, IModEnabledViewModel>()
            .AddViewModel<ModInstalledViewModel, IModInstalledViewModel>()
            .AddViewModel<ModNameViewModel, IModNameViewModel>()
            .AddViewModel<ModVersionViewModel, IModVersionViewModel>()
            .AddViewModel<MyGamesViewModel, IMyGamesViewModel>()
            .AddViewModel<NexusLoginOverlayViewModel, INexusLoginOverlayViewModel>()
            .AddViewModel<SpineViewModel, ISpineViewModel>()
            .AddViewModel<TopBarViewModel, ITopBarViewModel>()
            .AddViewModel<SpineDownloadButtonViewModel, ISpineDownloadButtonViewModel>()
            .AddViewModel<DownloadGameNameViewModel, IDownloadGameNameViewModel>()
            .AddViewModel<DownloadNameViewModel, IDownloadNameViewModel>()
            .AddViewModel<DownloadVersionViewModel, IDownloadVersionViewModel>()
            .AddViewModel<DownloadSizeViewModel, IDownloadSizeViewModel>()
            .AddViewModel<DownloadStatusViewModel, IDownloadStatusViewModel>()
            .AddViewModel<CancelDownloadOverlayViewModel, ICancelDownloadOverlayViewModel>()
            .AddViewModel<LoginMessageBoxViewModel, ILoginMessageBoxViewModel>()
            .AddViewModel<MessageBoxOkCancelViewModel, IMessageBoxOkCancelViewModel>()
            .AddViewModel<MetricsOptInViewModel, IMetricsOptInViewModel>()
            .AddViewModel<UpdaterViewModel, IUpdaterViewModel>()
            .AddViewModel<LoadoutLeftMenuViewModel, ILoadoutLeftMenuViewModel>()
            .AddViewModel<ModFilesViewModel, IModFilesViewModel>()
            .AddViewModel<ModInfoViewModel, IModInfoViewModel>()
            .AddViewModel<FileTreeNodeViewModel, IFileTreeNodeViewModel>()
            .AddViewModel<DummyLoadingViewModel, ILoadingViewModel>()
            .AddViewModel<DummyErrorViewModel, IErrorViewModel>()
            .AddViewModel<ApplyDiffViewModel, IApplyDiffViewModel>()

            // Views
            .AddView<DevelopmentBuildBannerView, IDevelopmentBuildBannerViewModel>()
            .AddView<DownloadsLeftMenuView, IDownloadsLeftMenuViewModel>()
            .AddView<GameWidget, IGameWidgetViewModel>()
            .AddView<HomeLeftMenuView, IHomeLeftMenuViewModel>()
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
            .AddView<Spine, ISpineViewModel>()
            .AddView<TopBarView, ITopBarViewModel>()
            .AddView<SpineDownloadButtonView, ISpineDownloadButtonViewModel>()
            .AddView<DownloadGameNameView, IDownloadGameNameViewModel>()
            .AddView<DownloadNameView, IDownloadNameViewModel>()
            .AddView<DownloadVersionView, IDownloadVersionViewModel>()
            .AddView<DownloadSizeView, IDownloadSizeViewModel>()
            .AddView<DownloadStatusView, IDownloadStatusViewModel>()
            .AddView<CancelDownloadOverlayView, ICancelDownloadOverlayViewModel>()
            .AddView<MessageBoxOkCancelView, IMessageBoxOkCancelViewModel>()
            .AddView<LoginMessageBoxView, ILoginMessageBoxViewModel>()
            .AddView<UpdaterView, IUpdaterViewModel>()
            .AddView<LoadoutLeftMenuView, ILoadoutLeftMenuViewModel>()
            .AddView<ApplyControlView, IApplyControlViewModel>()
            .AddView<ModFilesView, IModFilesViewModel>()
            .AddView<ModInfoView, IModInfoViewModel>()
            .AddView<FileTreeNodeView, IFileTreeNodeViewModel>()
            .AddView<LoadingView, ILoadingViewModel>()
            .AddView<ErrorView, IErrorViewModel>()
            .AddView<ApplyDiffView, IApplyDiffViewModel>()
            .AddView<FileTreeView, IFileTreeViewModel>()
            .AddView<FileOriginsPageView, IFileOriginsPageViewModel>()
            
            .AddView<SettingsView, ISettingsPageViewModel>()
            .AddViewModel<SettingsPageViewModel, ISettingsPageViewModel>()
            .AddView<SettingEntryView, ISettingEntryViewModel>()
            .AddViewModel<SettingEntryViewModel, ISettingEntryViewModel>()
            .AddView<SettingToggleControl, ISettingToggleViewModel>()
            .AddViewModel<SettingToggleViewModel, ISettingToggleViewModel>()
            .AddView<SettingComboBoxView, ISettingComboBoxViewModel>()
            .AddViewModel<SettingComboBoxViewModel, ISettingComboBoxViewModel>()

            .AddView<DiagnosticEntryView, IDiagnosticEntryViewModel>()
            .AddViewModel<DiagnosticEntryViewModel, IDiagnosticEntryViewModel>()
            .AddView<DiagnosticListView, IDiagnosticListViewModel>()
            .AddViewModel<DiagnosticListViewModel, IDiagnosticListViewModel>()
            .AddView<DiagnosticDetailsView, IDiagnosticDetailsViewModel>()
            .AddViewModel<DiagnosticDetailsViewModel, IDiagnosticDetailsViewModel>()

            .AddView<MarkdownRendererView, IMarkdownRendererViewModel>()
            .AddViewModel<MarkdownRendererViewModel, IMarkdownRendererViewModel>()
            .AddView<ChangelogPageView, IChangelogPageViewModel>()
            .AddViewModel<ChangelogPageViewModel, IChangelogPageViewModel>()

            .AddView<TextEditorPageView, ITextEditorPageViewModel>()
            .AddViewModel<TextEditorPageViewModel, ITextEditorPageViewModel>()

            .AddView<AlphaWarningView, IAlphaWarningViewModel>()
            .AddViewModel<AlphaWarningViewModel, IAlphaWarningViewModel>()

            // workspace system
            .AddSingleton<IWindowManager, WindowManager>()
            .AddRepository<WindowDataAttributes.Model>([WindowDataAttributes.Data])
            .AddAttributeCollection(typeof(WindowDataAttributes))
            .AddViewModel<WorkspaceViewModel, IWorkspaceViewModel>()
            .AddViewModel<PanelViewModel, IPanelViewModel>()
            .AddViewModel<AddPanelButtonViewModel, IAddPanelButtonViewModel>()
            .AddViewModel<AddPanelDropDownViewModel, IAddPanelDropDownViewModel>()
            .AddViewModel<PanelTabHeaderViewModel, IPanelTabHeaderViewModel>()
            .AddViewModel<NewTabPageViewModel, INewTabPageViewModel>()
            .AddViewModel<NewTabPageSectionViewModel, INewTabPageSectionViewModel>()
            .AddView<WorkspaceView, IWorkspaceViewModel>()
            .AddView<PanelView, IPanelViewModel>()
            .AddView<AddPanelButtonView, IAddPanelButtonViewModel>()
            .AddView<AddPanelDropDownView, IAddPanelDropDownViewModel>()
            .AddView<PanelTabHeaderView, IPanelTabHeaderViewModel>()
            .AddView<NewTabPageView, INewTabPageViewModel>()

            // page factories
            .AddSingleton<PageFactoryController>()
            .AddSingleton<IPageFactory, NewTabPageFactory>()
            .AddSingleton<IPageFactory, MyGamesPageFactory>()
            .AddSingleton<IPageFactory, LoadoutGridPageFactory>()
            .AddSingleton<IPageFactory, InProgressPageFactory>()
            .AddSingleton<IPageFactory, ModInfoPageFactory>()
            .AddSingleton<IPageFactory, DiagnosticListPageFactory>()
            .AddSingleton<IPageFactory, DiagnosticDetailsPageFactory>()
            .AddSingleton<IPageFactory, ApplyDiffPageFactory>()
            .AddSingleton<IPageFactory, SettingsPageFactory>()
            .AddSingleton<IPageFactory, ChangelogPageFactory>()
            .AddSingleton<IPageFactory, FileOriginsPageFactory>()
            .AddSingleton<IPageFactory, TextEditorPageFactory>()

            // LeftMenu factories
            .AddSingleton<ILeftMenuFactory, DownloadsLeftMenuFactory>()
            .AddSingleton<ILeftMenuFactory, HomeLeftMenuFactory>()
            .AddSingleton<ILeftMenuFactory, LoadoutLeftMenuFactory>()

            // Workspace Attachments
            .AddSingleton<IWorkspaceAttachmentsFactoryManager, WorkspaceAttachmentsFactoryManager>()
            .AddSingleton<IWorkspaceAttachmentsFactory, DownloadsAttachmentsFactory>()
            .AddSingleton<IWorkspaceAttachmentsFactory, HomeAttachmentsFactory>()
            .AddSingleton<IWorkspaceAttachmentsFactory, LoadoutAttachmentsFactory>()

            // Diagnostics
            .AddSingleton<IValueFormatter, ModReferenceFormatter>()
            .AddSingleton<IValueFormatter, LoadoutReferenceFormatter>()
            .AddSingleton<IValueFormatter, NamedLinkFormatter>()
            .AddSingleton<IDiagnosticWriter, DiagnosticWriter>()
            
            // Overlay Helpers
            .AddHostedService<NexusLoginOverlayService>()

            // Settings
            .AddUISettings()

            // Other
            .AddSingleton<InjectedViewLocator>()
            .AddFileSystem()

            .AddRepository<DownloadAnalysis.Model>([DownloadAnalysis.NumberOfEntries, DownloadAnalysis.SuggestedName])
            .AddRepository<StoredFile.Model>([StoredFile.Hash]);
    }

}
