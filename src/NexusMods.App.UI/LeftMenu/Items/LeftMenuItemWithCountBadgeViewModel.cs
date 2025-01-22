using System.Reactive.Disposables;
using System.Reactive.Linq;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LeftMenuItemWithCountBadgeViewModel : LeftMenuItemViewModel
{
    [Reactive] public bool IsBadgeVisible { get; private set; }
    [Reactive] public int Count { get; private set; }
    
    public IObservable<int> CountObservable { private get; init; } = Observable.Return(0);
    
    public LeftMenuItemWithCountBadgeViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData) : base(workspaceController, workspaceId, pageData)
    {
        this.WhenActivated(d =>
        {
            CountObservable
                .OnUI()
                .Subscribe(count =>
                {
                    Count = count;
                    IsBadgeVisible = count > 0;
                })
                .DisposeWith(d);
        });
    }
}
