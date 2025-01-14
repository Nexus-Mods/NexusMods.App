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
    [Reactive] public bool IsEnabled { get; private set; }
    
    [Reactive] public bool IsToggleVisible { get; private set; } = true;
    
    private CollectionGroupId _collectionGroupId;
    
    public CollectionLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData,
        IServiceProvider serviceProvider,
        CollectionGroupId collectionGroupId) : base(workspaceController, workspaceId, pageData)
    {
        _collectionGroupId = collectionGroupId;
        var conn = serviceProvider.GetRequiredService<IConnection>();

        var isEnabledObservable = CollectionGroup.Observe(conn, collectionGroupId)
            .Select(collectionGroup => collectionGroup.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled());
        
        this.WhenActivated(d =>
        {
            isEnabledObservable.Subscribe(isEnabled => IsEnabled = isEnabled)
                .DisposeWith(d);
        });

    }
}
