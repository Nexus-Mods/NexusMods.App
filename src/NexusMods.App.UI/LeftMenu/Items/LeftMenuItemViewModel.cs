using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.UI.Sdk;
using NexusMods.UI.Sdk.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LeftMenuItemViewModel : AViewModel<ILeftMenuItemViewModel>, ILeftMenuItemViewModel
{
    public StringComponent Text { get; init; } = new("");
    [Reactive] public IconValue Icon { get; set; } = new();
    [Reactive] public string ToolTipText { get; private set; }
    [Reactive] public bool IsActive { get; private set; }
    [Reactive] public bool IsSelected { get; private set; }
    public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; protected init; }
    public IReadOnlyList<IContextMenuItem> AdditionalContextMenuItems { get; protected init; } = Array.Empty<IContextMenuItem>();

    public StringComponent? ToolTip { get; init; }

    public LeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData? pageData
    )
    {
        IsActive = false;
        IsSelected = false;
        ToolTipText = string.Empty;

        IObservable<bool> isActiveObservable;
        IObservable<bool> isSelectedObservable;

        NavigateCommand = ReactiveCommand.Create<NavigationInformation>((info) =>
        {
            if (pageData is null) return;
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        });

        if (pageData is null)
        {
            isActiveObservable = Observable.Return(false);
            isSelectedObservable = Observable.Return(false);
        }
        else
        {
            var workspaceIsActiveObservable = workspaceController
                .WhenAnyValue(controller => controller.ActiveWorkspace)
                .Where(workspace => workspace.Id == workspaceId);

            // Should be 'Active' if the page is open and selected in any panel of the workspace
            isActiveObservable = workspaceIsActiveObservable.Select(workspace => workspace.Panels
                .ToObservableChangeSet()
                .FilterOnObservable(panel => panel
                    .WhenAnyValue(p => p.SelectedTab.Contents)
                    .Select(selectedTabContents =>
                    {
                        // The SelectedTab Contents can be null at startup
                        // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                        var context = selectedTabContents?.PageData?.Context;
                        var pageId = selectedTabContents?.PageData?.FactoryId;
                        // ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                        return pageData.FactoryId == pageId && pageData.Context.Equals(context);
                    })
                )
                .QueryWhenChanged(matchingPanels => matchingPanels.Count != 0)
                .DistinctUntilChanged()).Switch();

            var workspaceHasSinglePanelObservable = workspaceIsActiveObservable
                .Select(workspace => workspace.WhenAnyValue(w => w.Panels.Count))
                .Switch()
                .Select(panelCount => panelCount == 1)
                .DistinctUntilChanged()
                .Prepend(workspaceController.ActiveWorkspace.Panels.Count == 1);

            // Should be 'Selected' only if there are multiple panels and the page is open and selected in the selected panel 
            isSelectedObservable = workspaceIsActiveObservable
                .Select(workspace => workspace.WhenAnyValue(w => w.SelectedTab.Contents))
                .Switch()
                .Select(selectedTabContents =>
                    {
                        // The SelectedTab Contents can apparently be null at startup
                        var context = selectedTabContents?.PageData?.Context;
                        var pageId = selectedTabContents?.PageData?.FactoryId;
                        return pageData.FactoryId == pageId && pageData.Context.Equals(context);
                    }
                )
                .DistinctUntilChanged()
                .Prepend(pageData.Context.Equals(workspaceController.ActiveWorkspace.SelectedTab?.Contents?.PageData?.Context))
                // No Selected state if there is only one panel in the workspace
                .CombineLatest(workspaceHasSinglePanelObservable, (isSelected, hasSinglePanel) => isSelected && !hasSinglePanel)
                .DistinctUntilChanged();
        }

        this.WhenActivated(d =>
        {
            ToolTip?.Activate().DisposeWith(d);
            Text.Activate().DisposeWith(d);

            // Set the ToolTipText to the value of the ToolTip component if it is not null, otherwise set it to the value of the Text component
            (ToolTip ?? Text).Value.WhenAnyValue(item => item.Value)
                .Subscribe(value => ToolTipText = value)
                .DisposeWith(d);

            isActiveObservable.Subscribe(isActive => IsActive = isActive)
                .DisposeWith(d);

            isSelectedObservable.Subscribe(isSelected => IsSelected = isSelected)
                .DisposeWith(d);
        });
    }
}


