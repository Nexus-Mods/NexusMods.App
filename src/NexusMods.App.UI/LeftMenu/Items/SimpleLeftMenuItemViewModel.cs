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
    [Reactive] public string Text { get; set; }
    [Reactive] public IconValue Icon { get; set; }
    [Reactive] public ReactiveCommand<NavigationInformation, Unit> NavigateCommand { get; private set; }

    [Reactive] public bool IsActive { get; private set; }
    [Reactive] public bool IsSelected { get; private set; }

    public SimpleLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData
        )
    {
        Text = "VM Text";
        Icon = IconValues.LibraryOutline;
        IsActive = false;
        IsSelected = false;
        
        NavigateCommand = ReactiveCommand.Create<NavigationInformation>((info) =>
        {
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            workspaceController.OpenPage(workspaceId, pageData, behavior);
        });
        
        
        this.WhenActivated(d =>
        {
            var isActiveObservable = workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace)
                .Where(workspace => workspace.Id == workspaceId)
                .Select(workspace =>
                    {
                        return workspace.Panels.ToObservableChangeSet()
                            .QueryWhenChanged(panels =>
                                {
                                    return panels.Any(panel =>
                                        {
                                            var context = panel.SelectedTab.Contents.PageData.Context;
                                            var pageId = panel.SelectedTab.Contents.PageData.FactoryId;
                                            return pageId == pageData.FactoryId && context == pageData.Context;
                                        }
                                    );
                                }
                            );
                    }
                )
                .Switch();
            
            isActiveObservable.Subscribe(isActive => IsActive = isActive)
                .DisposeWith(d);
            
            var isSelectedObservable = workspaceController.WhenAnyValue(controller => controller.ActiveWorkspace)
                .Where(workspace => workspace.Id == workspaceId)
                .Select(workspace =>
                    {
                        return workspace.WhenAnyValue(w => w.SelectedTab);
                    }
                )
                .Switch()
                .Select(selectedTab =>
                {
                    var context = selectedTab.Contents.PageData.Context;
                    var pageId = selectedTab.Contents.PageData.FactoryId;
                    return pageId == pageData.FactoryId && context == pageData.Context;
                });
            
            var isSelectedDisposable = isSelectedObservable.Subscribe(isSelected => IsSelected = isSelected)
                .DisposeWith(d);

        });
    }
}
