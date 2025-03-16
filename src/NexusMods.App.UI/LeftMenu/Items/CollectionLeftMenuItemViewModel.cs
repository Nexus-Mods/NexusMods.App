using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class CollectionLeftMenuItemViewModel : LeftMenuItemViewModel, ILeftMenuItemWithToggleViewModel
{
    [Reactive] public bool IsEnabled { get; set; }

    public bool IsCollectionReadOnly { get; init; }
    public bool IsToggleVisible => true;

    public ReactiveCommand<Unit, Unit> ToggleIsEnabledCommand { get; }
    
    public CollectionGroupId CollectionGroupId { get; }
    
    public CollectionLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData,
        IServiceProvider serviceProvider,
        CollectionGroupId collectionGroupId) : base(workspaceController, workspaceId, pageData)
    {
        var conn = serviceProvider.GetRequiredService<IConnection>();
        
        CollectionGroupId = collectionGroupId;

        var isEnabledObservable = CollectionGroup.Observe(conn, collectionGroupId)
            .Select(collectionGroup => collectionGroup.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled());
        
        ToggleIsEnabledCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            using var tx = conn.BeginTransaction();
            
            if (IsEnabled)
            {
                tx.Retract(CollectionGroupId.Value, LoadoutItem.Disabled, Null.Instance);
            } else
            {
                tx.Add(CollectionGroupId.Value, LoadoutItem.Disabled, Null.Instance);
            }
            
            await tx.Commit();
        });
        
        this.WhenActivated(d =>
        {
            isEnabledObservable
                .OnUI()
                .Subscribe(isEnabled => IsEnabled = isEnabled)
                .DisposeWith(d);
        });

    }
}
