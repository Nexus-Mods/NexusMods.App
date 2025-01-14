using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class CollectionLeftMenuItemViewModel : LeftMenuItemViewModel
{
    [Reactive] public override bool IsEnabled { get; set; }
    
    public override bool IsToggleVisible { get; } = true;
    
    public CollectionGroupId CollectionGroupId;
    
    public CollectionLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData,
        IServiceProvider serviceProvider,
        CollectionGroupId collectionGroupId) : base(workspaceController, workspaceId, pageData)
    {
        IsToggleVisible = true;
        CollectionGroupId = collectionGroupId;
        var conn = serviceProvider.GetRequiredService<IConnection>();

        var isEnabledObservable = CollectionGroup.Observe(conn, collectionGroupId)
            .Select(collectionGroup => collectionGroup.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled());
        
        this.WhenActivated(d =>
        {
            isEnabledObservable
                .OnUI()
                .Subscribe(isEnabled => IsEnabled = isEnabled)
                .DisposeWith(d);
        });

    }
}
