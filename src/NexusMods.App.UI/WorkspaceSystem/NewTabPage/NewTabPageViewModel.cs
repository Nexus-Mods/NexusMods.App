using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class NewTabPageViewModel : APageViewModel<INewTabPageViewModel>, INewTabPageViewModel
{
    private readonly SourceList<INewTabPageSectionItemViewModel> _itemSource = new();

    private readonly ReadOnlyObservableCollection<INewTabPageSectionViewModel> _sections;
    public ReadOnlyObservableCollection<INewTabPageSectionViewModel> Sections => _sections;

    public AlertSettingsWrapper AlertSettingsWrapper { get; }

    public NewTabPageViewModel(
        ISettingsManager settingsManager,
        IWindowManager windowManager,
        PageDiscoveryDetails[] discoveryDetails) : base(windowManager)
    {
        TabTitle = Language.PanelTabHeaderViewModel_Title_New_Tab;
        TabIcon = IconValues.Tab;

        AlertSettingsWrapper = new AlertSettingsWrapper(settingsManager, "add panels using add-panel button");

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

            if (!AlertSettingsWrapper.IsDismissed)
            {
                // dismiss the banner if the user adds a panel
                workspace.Panels
                    .ObserveCollectionChanges()
                    .Where(_ => !AlertSettingsWrapper.IsDismissed)
                    .Select(_ => workspace.Panels)
                    .Prepend(workspace.Panels)
                    .Select(x => x.Count)
                    .Where(panelCount => panelCount > 1)
                    .Subscribe(_ => AlertSettingsWrapper.DismissAlert())
                    .DisposeWith(disposables);
            }

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
                        behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                    }

                    workspaceController.OpenPage(WorkspaceId, pageData, behavior, checkOtherPanels: false);
                })
                .DisposeWith(disposables);
        });
    }
}
