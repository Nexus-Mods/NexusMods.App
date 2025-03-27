using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.EventBus;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Serialization.Json;
using NexusMods.App.UI.Controls.DevelopmentBuildBanner;
using NexusMods.App.UI.Controls.Diagnostics;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Controls.LoadoutBadge;
using NexusMods.App.UI.Controls.LoadoutCard;
using NexusMods.App.UI.Controls.MarkdownRenderer;
using NexusMods.App.UI.Controls.MiniGameWidget;
using NexusMods.App.UI.Controls.Settings.Section;
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
using NexusMods.App.UI.LeftMenu.Home;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.LeftMenu.Loadout;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Overlays.AlphaWarning;
using NexusMods.App.UI.Overlays.Generic.MessageBox.Ok;
using NexusMods.App.UI.Overlays.Generic.MessageBox.OkCancel;
using NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;
using NexusMods.App.UI.Overlays.Login;
using NexusMods.App.UI.Overlays.ManageGameWarning;
using NexusMods.App.UI.Overlays.MetricsOptIn;
using NexusMods.App.UI.Overlays.Updater;
using NexusMods.App.UI.Pages;
using NexusMods.App.UI.Pages.Changelog;
using NexusMods.App.UI.Pages.CollectionDownload;
using NexusMods.App.UI.Pages.DebugControls;
using NexusMods.App.UI.Pages.Diagnostics;
using NexusMods.App.UI.Pages.Diff.ApplyDiff;
using NexusMods.App.UI.Pages.ItemContentsFileTree;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LibraryPage.Collections;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Pages.MyLoadouts;
using NexusMods.App.UI.Pages.ObservableInfo;
using NexusMods.App.UI.Pages.Settings;
using NexusMods.App.UI.Pages.Sorting;
using NexusMods.App.UI.Pages.TextEdit;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceAttachments;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;
using ReactiveUI;
using ImageButton = NexusMods.App.UI.Controls.Spine.Buttons.Image.ImageButton;
using ManageGameWarningView = NexusMods.App.UI.Overlays.ManageGameWarning.ManageGameWarningView;
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
            .AddSingleton<GameRunningTracker>()
            .AddTransient<MainWindow>()

            // Services
            .AddSingleton<IOverlayController, OverlayController>()

            // View Models
            .AddTransient<MainWindowViewModel>()
            .AddSingleton<IViewLocator, InjectedViewLocator>()
            
            .AddViewModel<CollectionCardDesignViewModel, ICollectionCardViewModel>()

            .AddViewModel<DevelopmentBuildBannerViewModel, IDevelopmentBuildBannerViewModel>()
            .AddViewModel<GameWidgetViewModel, IGameWidgetViewModel>()
            .AddViewModel<MiniGameWidgetViewModel, IMiniGameWidgetViewModel>()
            .AddViewModel<HomeLeftMenuViewModel, IHomeLeftMenuViewModel>()
            .AddViewModel<IconButtonViewModel, IIconButtonViewModel>()
            .AddViewModel<LeftMenuItemViewModel, ILeftMenuItemViewModel>()
            .AddViewModel<CollectionLeftMenuItemViewModel, ILeftMenuItemViewModel>()
            .AddViewModel<ImageButtonViewModel, IImageButtonViewModel>()
            .AddViewModel<LaunchButtonViewModel, ILaunchButtonViewModel>()
            .AddViewModel<ApplyControlViewModel, IApplyControlViewModel>()
            .AddViewModel<MyGamesViewModel, IMyGamesViewModel>()
            .AddViewModel<NexusLoginOverlayViewModel, INexusLoginOverlayViewModel>()
            .AddViewModel<SpineViewModel, ISpineViewModel>()
            .AddViewModel<TopBarViewModel, ITopBarViewModel>()
            .AddViewModel<SpineDownloadButtonViewModel, ISpineDownloadButtonViewModel>()
            .AddViewModel<MessageBoxOkViewModel, IMessageBoxOkViewModel>()
            .AddViewModel<LoginMessageBoxViewModel, ILoginMessageBoxViewModel>()
            .AddViewModel<MessageBoxOkCancelViewModel, IMessageBoxOkCancelViewModel>()
            .AddViewModel<MetricsOptInViewModel, IMetricsOptInViewModel>()
            .AddViewModel<UpdaterViewModel, IUpdaterViewModel>()
            .AddViewModel<LoadoutLeftMenuViewModel, ILoadoutLeftMenuViewModel>()
            .AddViewModel<FileTreeNodeViewModel, IFileTreeNodeViewModel>()
            .AddViewModel<ApplyDiffViewModel, IApplyDiffViewModel>()
            .AddViewModel<ManageGameWarningViewModel, IManageGameWarningViewModel>()

            // Views
            .AddView<CollectionCardView, ICollectionCardViewModel>()
            .AddView<DevelopmentBuildBannerView, IDevelopmentBuildBannerViewModel>()
            .AddView<GameWidget, IGameWidgetViewModel>()
            .AddView<MiniGameWidget, IMiniGameWidgetViewModel>()
            .AddView<HomeLeftMenuView, IHomeLeftMenuViewModel>()
            .AddView<IconButton, IIconButtonViewModel>()
            .AddView<LeftMenuItemView, ILeftMenuItemViewModel>()
            .AddView<ImageButton, IImageButtonViewModel>()
            .AddView<LaunchButtonView, ILaunchButtonViewModel>()
            .AddView<MetricsOptInView, IMetricsOptInViewModel>()
            .AddView<MyGamesView, IMyGamesViewModel>()
            .AddView<NexusLoginOverlayView, INexusLoginOverlayViewModel>()
            .AddView<Spine, ISpineViewModel>()
            .AddView<TopBarView, ITopBarViewModel>()
            .AddView<SpineDownloadButtonView, ISpineDownloadButtonViewModel>()
            .AddView<MessageBoxOkView, IMessageBoxOkViewModel>()
            .AddView<MessageBoxOkCancelView, IMessageBoxOkCancelViewModel>()
            .AddView<LoginMessageBoxView, ILoginMessageBoxViewModel>()
            .AddView<ManageGameWarningView, IManageGameWarningViewModel>()
            .AddView<UpdaterView, IUpdaterViewModel>()
            .AddView<LoadoutLeftMenuView, ILoadoutLeftMenuViewModel>()
            .AddView<ApplyControlView, IApplyControlViewModel>()
            .AddView<FileTreeNodeView, IFileTreeNodeViewModel>()
            .AddView<ApplyDiffView, IApplyDiffViewModel>()
            .AddView<FileTreeView, IFileTreeViewModel>()
            
            .AddView<MyLoadoutsView, IMyLoadoutsViewModel>()
            .AddViewModel<MyLoadoutsViewModel, IMyLoadoutsViewModel>()
            .AddView<LoadoutCardView, ILoadoutCardViewModel>()
            .AddView<CreateNewLoadoutCardView, ICreateNewLoadoutCardViewModel>()
            .AddViewModel<LoadoutBadgeViewModel, ILoadoutBadgeViewModel>()
            
            .AddView<SettingsView, ISettingsPageViewModel>()
            .AddViewModel<SettingsPageViewModel, ISettingsPageViewModel>()

            .AddView<SettingSectionView, ISettingSectionViewModel>()
            .AddViewModel<SettingSectionViewModel, ISettingSectionViewModel>()

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

            .AddView<LibraryItemDeleteConfirmationView, ILibraryItemDeleteConfirmationViewModel>()
            .AddViewModel<LibraryItemDeleteConfirmationViewModel, ILibraryItemDeleteConfirmationViewModel>()
            
            .AddView<AlphaWarningView, IAlphaWarningViewModel>()
            .AddViewModel<AlphaWarningViewModel, IAlphaWarningViewModel>()

            .AddView<ItemContentsFileTreeView, IItemContentsFileTreeViewModel>()
            .AddViewModel<ItemContentsFileTreeViewModel, IItemContentsFileTreeViewModel>()

            .AddView<LibraryView, ILibraryViewModel>()
            .AddView<LoadoutView, ILoadoutViewModel>()

            .AddView<CollectionDownloadView, ICollectionDownloadViewModel>()
            .AddViewModel<CollectionDownloadViewModel, ICollectionDownloadViewModel>()
            
            .AddView<LoadOrderView, ILoadOrderViewModel>()
            .AddViewModel<LoadOrderViewModel, ILoadOrderViewModel>()
            
            .AddView<LoadOrdersWIPPageView,ILoadOrdersWIPPageViewModel>()
            .AddViewModel<LoadOrdersWipPageViewModel, ILoadOrdersWIPPageViewModel>()

            .AddView<UpgradeToPremiumView, IUpgradeToPremiumViewModel>()
            .AddViewModel<UpgradeToPremiumViewModel, IUpgradeToPremiumViewModel>()

            .AddView<CollectionLoadoutView, ICollectionLoadoutViewModel>()
            .AddViewModel<CollectionLoadoutViewModel, ICollectionLoadoutViewModel>()

            .AddView<ObservableInfoPageView, IObservableInfoPageViewModel>()
            .AddViewModel<ObservableInfoPageViewModel, IObservableInfoPageViewModel>()
            
            .AddView<DebugControlsPageView, IDebugControlsPageViewModel>()
            .AddViewModel<DebugControlsPageViewModel, IDebugControlsPageViewModel>()

            .AddView<ManualDownloadRequiredOverlayView, IManualDownloadRequiredOverlayViewModel>()
            .AddViewModel<ManualDownloadRequiredOverlayViewModel, IManualDownloadRequiredOverlayViewModel>()

            .AddView<RemoveGameOverlayView, IRemoveGameOverlayViewModel>()
            .AddViewModel<RemoveGameOverlayViewModel, IRemoveGameOverlayViewModel>()

            // workspace system
            .AddSingleton<IWindowManager, WindowManager>()
            .AddWindowDataAttributesModel()
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
            .AddSingleton<IPageFactory, DiagnosticListPageFactory>()
            .AddSingleton<IPageFactory, DiagnosticDetailsPageFactory>()
            .AddSingleton<IPageFactory, ApplyDiffPageFactory>()
            .AddSingleton<IPageFactory, SettingsPageFactory>()
            .AddSingleton<IPageFactory, ChangelogPageFactory>()
            .AddSingleton<IPageFactory, TextEditorPageFactory>()
            .AddSingleton<IPageFactory, MyLoadoutsPageFactory>()
            .AddSingleton<IPageFactory, ItemContentsFileTreePageFactory>()
            .AddSingleton<IPageFactory, LibraryPageFactory>()
            .AddSingleton<IPageFactory, LoadoutPageFactory>()
            .AddSingleton<IPageFactory, CollectionDownloadPageFactory>()
            .AddSingleton<IPageFactory, LoadOrdersWIPPageFactory>()
            .AddSingleton<IPageFactory, CollectionLoadoutPageFactory>()
            .AddSingleton<IPageFactory, ObservableInfoPageFactory>()
            .AddSingleton<IPageFactory, DebugControlsPageFactory>()

            // LeftMenu factories
            .AddSingleton<ILeftMenuFactory, HomeLeftMenuFactory>()
            .AddSingleton<ILeftMenuFactory, LoadoutLeftMenuFactory>()

            // Workspace Attachments
            .AddSingleton<IWorkspaceAttachmentsFactoryManager, WorkspaceAttachmentsFactoryManager>()
            .AddSingleton<IWorkspaceAttachmentsFactory, DownloadsAttachmentsFactory>()
            .AddSingleton<IWorkspaceAttachmentsFactory, HomeAttachmentsFactory>()
            .AddSingleton<IWorkspaceAttachmentsFactory, LoadoutAttachmentsFactory>()

            // Diagnostics
            .AddDiagnosticWriter()

            // Overlay Helpers
            .AddHostedService<NexusLoginOverlayService>()

            // Settings
            .AddUISettings()

            // Other
            .AddSingleton<InjectedViewLocator>()
            .AddSingleton<CollectionDataProvider>()
            .AddSingleton<ILibraryDataProvider, LocalFileDataProvider>()
            .AddSingleton<ILoadoutDataProvider, LocalFileDataProvider>()
            .AddSingleton<ILibraryDataProvider, NexusModsDataProvider>()
            .AddSingleton<ILoadoutDataProvider, NexusModsDataProvider>()
            .AddSingleton<ILoadoutDataProvider, BundledDataProvider>()
            .AddSingleton<ILoadOrderDataProvider, LoadOrderDataProvider>()
            .AddSingleton<IEventBus, EventBus>()
            .AddSingleton<IAvaloniaInterop, AvaloniaInterop>()
            .AddSingleton<UpdateChecker>()
            .AddFileSystem()
            .AddImagePipelines();
    }

}
