using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Banners;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageViewModel : APageViewModel<INewTabPageViewModel>, INewTabPageViewModel
{
    private readonly SourceList<INewTabPageSectionItemViewModel> _itemSource = new();

    private readonly ReadOnlyObservableCollection<INewTabPageSectionViewModel> _sections;
    public ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections => _sections;

    [Reactive] public IconValue StateIcon { get; [UsedImplicitly] private set; } = new();

    public BannerSettingsWrapper BannerSettingsWrapper { get; }

    public NewTabPageViewModel(
        ISettingsManager settingsManager,
        IWindowManager windowManager,
        PageDiscoveryDetails[] discoveryDetails) : base(windowManager)
    {
        TabTitle = Language.PanelTabHeaderViewModel_Title_New_Tab;
        TabIcon = IconValues.Tab;

        BannerSettingsWrapper = new BannerSettingsWrapper(settingsManager, "add panels using add-panel button");

        _itemSource.Edit(list =>
        {
            var toAdd = discoveryDetails
                .Select(details => (INewTabPageSectionItemViewModel)new NewTabPageSectionItemViewModel(details));

            list.AddRange(toAdd);
        });

        _itemSource
            .Connect()
            .GroupOn(item => item.SectionName)
            .Transform(x => (INewTabPageSectionViewModel)new NewTabPageSectionViewModel(x.GroupKey, x.List))
            .Bind(out _sections)
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            var workspace = GetWorkspaceController().ActiveWorkspace;
            Debug.Assert(workspace is not null);

            if (!BannerSettingsWrapper.IsDismissed)
            {
                // dismiss the banner if the user adds a panel
                workspace.Panels
                    .ObserveCollectionChanges()
                    .Where(_ => !BannerSettingsWrapper.IsDismissed)
                    .Select(_ => workspace.Panels)
                    .Prepend(workspace.Panels)
                    .Select(x => x.Count)
                    .Where(panelCount => panelCount > 1)
                    .Subscribe(_ => BannerSettingsWrapper.DismissBanner())
                    .DisposeWith(disposables);
            }

            // Use the same icon in the banner as in the TopBar
            workspace.AddPanelButtonViewModels
                .ObserveCollectionChanges()
                .Select(_ => workspace.AddPanelButtonViewModels)
                .Prepend(workspace.AddPanelButtonViewModels)
                .Where(x => x.Count > 0)
                .Select(x => x.First().ButtonImage)
                .Select(image => new IconValue(new AvaloniaImage(image)))
                .BindToVM(this, vm => vm.StateIcon)
                .DisposeWith(disposables);

            _itemSource
                .Connect()
                .MergeMany(item => item.SelectItemCommand)
                .SubscribeWithErrorLogging(tuple =>
                {
                    var (pageData, info) = tuple;
                    var workspaceController = GetWorkspaceController();

                    OpenPageBehavior behavior;
                    if (!info.OpenPageBehaviorType.HasValue && info.Input.IsPrimaryInput())
                    {
                        // NOTE(erri120): default should be replacing this tab
                        behavior = new OpenPageBehavior.ReplaceTab(PanelId, TabId);
                    }
                    else
                    {
                        behavior = workspaceController.GetOpenPageBehavior(pageData, info, IdBundle);
                    }

                    workspaceController.OpenPage(WorkspaceId, pageData, behavior);
                })
                .DisposeWith(disposables);
        });
    }
}
