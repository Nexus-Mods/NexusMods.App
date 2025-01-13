using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class SimpleLeftMenuItemViewModel : AViewModel<INewLeftMenuItemViewModel>, INewLeftMenuItemViewModel
{
    [Reactive] public string Text { get; set; } = "";
    [Reactive] public IconValue Icon { get; set; } = new();
    [Reactive] public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; private set; }

    [Reactive] public bool IsActive { get; private set; }
    [Reactive] public bool IsSelected { get; private set; }

    public SimpleLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData
    )
    {
        IsActive = false;
        IsSelected = false;

        NavigateCommand = ReactiveCommand.Create<NavigationInformation>((info) =>
            {
                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(workspaceId, pageData, behavior);
            }
        );

        var workspaceIsActiveObservable = workspaceController
            .WhenAnyValue(controller => controller.ActiveWorkspace)
            .Where(workspace => workspace.Id == workspaceId);

        var isActiveObservable = workspaceIsActiveObservable
            .Select(workspace =>
                workspace.Panels.ToObservableChangeSet()
                    .FilterOnObservable(panel =>
                        panel.WhenAnyValue(p => p.SelectedTab.Contents)
                            .Select(selectedTabContents =>
                                {
                                    // The SelectedTab Contents can be null at startup
                                    // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                                    var context = selectedTabContents?.PageData?.Context;
                                    var pageId = selectedTabContents?.PageData?.FactoryId;
                                    // ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                                    return pageData.FactoryId == pageId && pageData.Context.Equals(context);
                                }
                            )
                    )
                    .QueryWhenChanged(matchingPanels => matchingPanels.Count != 0)
            )
            .Switch();
        
        var workspaceHasSinglePanelObservable = workspaceIsActiveObservable
            .Select(workspace => workspace.WhenAnyValue(w => w.Panels.Count))
            .Switch()
            .Select(panelCount => panelCount == 1)
            .Prepend(workspaceController.ActiveWorkspace.Panels.Count == 1);
        
        var isSelectedObservable = workspaceIsActiveObservable
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
            .Prepend(pageData.Context.Equals(workspaceController.ActiveWorkspace.SelectedTab?.Contents?.PageData?.Context))
            // No Selected state if there is only one panel in the workspace
            .CombineLatest(workspaceHasSinglePanelObservable, (isSelected, hasSinglePanel) => isSelected && !hasSinglePanel);

        this.WhenActivated(d =>
            {
                isActiveObservable.Subscribe(isActive => IsActive = isActive)
                    .DisposeWith(d);

                isSelectedObservable.Subscribe(isSelected => IsSelected = isSelected)
                    .DisposeWith(d);
            }
        );
    }
}
