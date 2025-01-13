using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
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

        var isActiveObservable = workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace)
            .Where(workspace => workspace.Id == workspaceId)
            .Select(workspace =>
                workspace.Panels.ToObservableChangeSet()
                    .FilterOnObservable(panel =>
                        panel.WhenAnyValue(p => p.SelectedTab.Contents)
                            .Select(selectedTabContents =>
                                {
                                    // The SelectedTab Contents can be null at startup
                                    var context = selectedTabContents?.PageData?.Context;
                                    var pageId = selectedTabContents?.PageData?.FactoryId;
                                    return pageData.FactoryId == pageId && pageData.Context.Equals(context);
                                }
                            )
                    )
                    .QueryWhenChanged(matchingPanels => matchingPanels.Count != 0)
            )
            .Switch();

        var isSelectedObservable = workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace)
            .Where(workspace => workspace.Id == workspaceId)
            .Select(workspace => workspace.WhenAnyValue(w => w.SelectedTab.Contents))
            .Switch()
            .Select(selectedTabContents =>
                {
                    // The SelectedTab Contents can apparently be null at startup
                    var context = selectedTabContents?.PageData?.Context;
                    var pageId = selectedTabContents?.PageData?.FactoryId;
                    return pageData.FactoryId == pageId && pageData.Context.Equals(context);
                }
            );

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
