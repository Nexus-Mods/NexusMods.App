using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Collections;
using NexusMods.App.UI.CollectionDeleteService;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.UI.Sdk.Icons;
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
    
    private readonly ICollectionDeleteService _collectionDeleteService;
    private readonly IConnection _connection;
    private readonly IWorkspaceController _workspaceController;
    private readonly WorkspaceId _workspaceId;
    private readonly bool _isNexusCollection;

    public CollectionLeftMenuItemViewModel(
        IWorkspaceController workspaceController,
        WorkspaceId workspaceId,
        PageData pageData,
        IServiceProvider serviceProvider,
        CollectionGroupId collectionGroupId) : base(workspaceController, workspaceId, pageData)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _collectionDeleteService = serviceProvider.GetRequiredService<ICollectionDeleteService>();
        _workspaceController = workspaceController;
        _workspaceId = workspaceId;
        
        CollectionGroupId = collectionGroupId;

        // Detect collection type and create delete context menu item
        var collectionGroup = CollectionGroup.Load(_connection.Db, CollectionGroupId);
        _isNexusCollection = collectionGroup.TryGetAsNexusCollectionLoadoutGroup(out _);
        var deleteContextMenuItem = CreateDeleteContextMenuItem();

        var isEnabledObservable = CollectionGroup.Observe(_connection, collectionGroupId)
            .Select(collGroup => collGroup.AsLoadoutItemGroup().AsLoadoutItem().IsEnabled());
        
        ToggleIsEnabledCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            using var tx = _connection.BeginTransaction();
            
            if (IsEnabled)
            {
                tx.Retract(CollectionGroupId.Value, LoadoutItem.Disabled, Null.Instance);
            } else
            {
                tx.Add(CollectionGroupId.Value, LoadoutItem.Disabled, Null.Instance);
            }
            
            await tx.Commit();
        });
        
        // Set additional context menu items
        AdditionalContextMenuItems = [deleteContextMenuItem];
        
        this.WhenActivated(d =>
        {
            isEnabledObservable
                .OnUI()
                .Subscribe(isEnabled => IsEnabled = isEnabled)
                .DisposeWith(d);
        });

    }
    
    private IContextMenuItem CreateDeleteContextMenuItem()
    {
        var deleteCommand = CreateDeleteCommand();
        
        var header = _isNexusCollection 
            ? Language.CollectionLoadoutView_UninstallCollection 
            : Language.Loadout_DeleteCollection_Menu_Text;
            
        var icon = _isNexusCollection 
            ? IconValues.PlaylistRemove 
            : IconValues.DeleteOutline;
        
        return new ContextMenuItem
        {
            Header = header,
            Icon = icon,
            Command = deleteCommand,
            IsVisible = true,
        };
    }
    
    private ReactiveCommand<Unit, Unit> CreateDeleteCommand()
    {
        // Nexus collections can always be uninstalled, regular collections follow CanDeleteCollection logic
        var canExecute = _isNexusCollection 
            ? Observable.Return(true)
            : _collectionDeleteService.ObserveCanDeleteCollection(CollectionGroupId);

        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (_isNexusCollection)
            {
                var collectionGroup = CollectionGroup.Load(_connection.Db, CollectionGroupId);
                if (collectionGroup.TryGetAsNexusCollectionLoadoutGroup(out var nexusCollectionGroup))
                {
                    // For Nexus collections, we need to navigate away before deletion
                    await _collectionDeleteService.DeleteNexusCollectionAsync(
                        nexusCollectionGroup, 
                        _workspaceController
                    );
                }
            }
            else
            {
                // For regular collections, just delete without navigation
                await _collectionDeleteService.DeleteCollectionAsync(CollectionGroupId);
            }
        }, canExecute: canExecute);
    }
}
