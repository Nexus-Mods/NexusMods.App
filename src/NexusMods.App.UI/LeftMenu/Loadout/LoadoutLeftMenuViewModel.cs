using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Diagnostics;
using NexusMods.App.UI.Pages.ItemContentsFileTree;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Collections;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public WorkspaceId WorkspaceId { get; }
    
    public ILeftMenuItemViewModel LeftMenuItemLibrary { get; }
    public ILeftMenuItemViewModel LeftMenuItemLoadout { get; }
    public ILeftMenuItemViewModel LeftMenuItemHealthCheck { get; }
    [Reactive] public ILeftMenuItemViewModel? LeftMenuItemExternalChanges { get; private set; }
    
    public IApplyControlViewModel ApplyControlViewModel { get; }

    private ReadOnlyObservableCollection<ILeftMenuItemViewModel> _leftMenuCollectionItems = new([]);
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> LeftMenuCollectionItems => _leftMenuCollectionItems;
    
    [Reactive] private int NewDownloadModelCount { get; set; }

    public LoadoutLeftMenuViewModel(
        LoadoutContext loadoutContext,
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        WorkspaceId = workspaceId;

        var diagnosticManager = serviceProvider.GetRequiredService<IDiagnosticManager>();
        var conn = serviceProvider.GetRequiredService<IConnection>();
        var monitor = serviceProvider.GetRequiredService<IJobMonitor>();
        var overlayController = serviceProvider.GetRequiredService<IOverlayController>();
        var gameRunningTracker = serviceProvider.GetRequiredService<GameRunningTracker>();
        var logger = serviceProvider.GetRequiredService<ILogger<LoadoutLeftMenuViewModel>>();

        var collectionItemComparer = new LeftMenuCollectionItemComparer();
        var collectionDownloader = new CollectionDownloader(serviceProvider);
        
        // Library
        LeftMenuItemLibrary = new LeftMenuItemWithCountBadgeViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = LibraryPageFactory.StaticId,
                Context = new LibraryPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                },
            }
        )
        {
            Text = new StringComponent(Language.LibraryPageTitle),
            Icon = IconValues.LibraryOutline,
            ToolTip = new StringComponent(Language.LibraryPageTitleToolTip),
            CountObservable = this.WhenAnyValue(vm => vm.NewDownloadModelCount),
        };
        
        // Loadout
        var loadoutModCountObservable = conn.ObserveDatoms(LibraryLinkedLoadoutItem.PrimaryAttribute)
            .Filter(datom =>
                {
                    var item = LoadoutItem.Load(conn.Db, datom.E);
                    return item.LoadoutId.Equals(loadoutContext.LoadoutId);
                }
            )
            .QueryWhenChanged(datoms => datoms.Count);

        var loadoutLabelObservable = loadoutModCountObservable.Select(count =>
            string.Format(Language.LoadoutView_Title_Installed_Mods, count)
        );

        LeftMenuItemLoadout = new LeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = LoadoutPageFactory.StaticId,
                Context = new LoadoutPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                    GroupScope = Optional<LoadoutItemGroupId>.None,
                },
            }
        )
        {
            Text = new StringComponent(Language.LoadoutView_Title_Installed_Mods_Default, loadoutLabelObservable),
            Icon = IconValues.FormatAlignJustify,
            ToolTip = new StringComponent(Language.LoadoutView_Title_Installed_Mods_ToolTip),
        };

        // Collections
        var collectionItemsObservable = CollectionGroup.ObserveAll(conn)
            .FilterImmutable(f => f.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutContext.LoadoutId)
            .FilterOnObservable(group =>
                {
                    if (!group.TryGetAsNexusCollectionLoadoutGroup(out var nexusCollection))
                        return Observable.Return(true);
                    return collectionDownloader.IsCollectionInstalledObservable(
                        nexusCollection.Revision,
                        Observable.Return(Optional<CollectionGroup.ReadOnly>.Create(group))
                    );
                }
            )
            .SortBy(item => item.IsReadOnly)
            .Transform(collection =>
            {
                var pageData = collection.IsReadOnly
                    ? new PageData
                    {
                        FactoryId = CollectionLoadoutPageFactory.StaticId,
                        Context = new CollectionLoadoutPageContext
                        {
                            LoadoutId = collection.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId,
                            GroupId = collection,
                        },
                    }
                    : new PageData
                    {
                        FactoryId = LoadoutPageFactory.StaticId,
                        Context = new LoadoutPageContext
                        {
                            LoadoutId = collection.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId,
                            GroupScope = collection.AsLoadoutItemGroup().LoadoutItemGroupId,
                        },
                    };

                return new CollectionLeftMenuItemViewModel(
                    workspaceController,
                    WorkspaceId,
                    pageData,
                    serviceProvider,
                    collection.CollectionGroupId
                )
                {
                    Text = new StringComponent(collection.AsLoadoutItemGroup().AsLoadoutItem().Name),
                    Icon = IconValues.CollectionsOutline,
                };
            })
            .Transform(ILeftMenuItemViewModel (item) => item);
        
        // Health Check
        LeftMenuItemHealthCheck = new HealthCheckLeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = DiagnosticListPageFactory.StaticId,
                Context = new DiagnosticListPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                },
            },
            diagnosticManager,
            loadoutContext.LoadoutId
        )
        {
            Text = new StringComponent(Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics),
            Icon = IconValues.Cardiology,
            ToolTip = new StringComponent(Language.LoadoutLeftMenuViewModel_Diagnostics_ToolTip),
        };
        
        // Apply Control
        ApplyControlViewModel = new ApplyControlViewModel(loadoutContext.LoadoutId,
            serviceProvider,
            monitor,
            overlayController,
            gameRunningTracker
        );
        

        this.WhenActivated(disposable =>
            {
                // External Changes
                conn.ObserveDatoms(LoadoutOverridesGroup.OverridesFor, loadoutContext.LoadoutId)
                    .AsEntityIds()
                    .Transform(datom => LoadoutOverridesGroup.Load(conn.Db, datom.E))
                    .QueryWhenChanged(overrides =>
                    {
                        if (overrides.Count == 0)
                        {
                            return null;
                        }

                        Debug.Assert(overrides.Count == 1, "There should only be one LoadoutOverridesGroup for a LoadoutId");
                            
                        var group = overrides.Items.First().AsLoadoutItemGroup();
                             
                        return new LeftMenuItemViewModel(
                            workspaceController,
                            WorkspaceId,
                            new PageData
                            {
                                FactoryId = ItemContentsFileTreePageFactory.StaticId,
                                Context = new ItemContentsFileTreePageContext
                                {
                                    GroupId = group.LoadoutItemGroupId,
                                },
                            }
                        )
                        {
                            Text = new StringComponent(Language.LoadoutLeftMenuViewModel_External_Changes),
                            Icon = IconValues.FolderEditOutline,
                            ToolTip = new StringComponent(Language.LoadoutLeftMenuViewModel_External_Changes_ToolTip),
                        };

                    })
                    .OnUI()
                    .SubscribeWithErrorLogging(item => LeftMenuItemExternalChanges = item)
                    .DisposeWith(disposable);
                
                
                collectionItemsObservable
                    .OnUI()
                    .SortAndBind(out _leftMenuCollectionItems, collectionItemComparer)
                    .Subscribe()
                    .DisposeWith(disposable);

                LibraryUserFilters.ObserveFilteredLibraryItems(connection: conn)
                    .RemoveKey()
                    .WhereReasonsAre(ListChangeReason.Add,
                        ListChangeReason.AddRange,
                        ListChangeReason.Remove,
                        ListChangeReason.RemoveRange
                    )
                    .OnUI()
                    .SubscribeWithErrorLogging(changeSet => NewDownloadModelCount = Math.Max(0, NewDownloadModelCount + (changeSet.Adds - changeSet.Removes)))
                    .DisposeWith(disposable);

                // NOTE(erri120): No new downloads when the Left Menu gets loaded. Must be set here because the observable stream
                // above will count all existing downloads, which we want to ignore.
                NewDownloadModelCount = 0;
                
                LeftMenuItemLibrary.WhenAnyValue(item=> item.IsActive)
                    .Subscribe(isActive => NewDownloadModelCount = isActive ? 0 : NewDownloadModelCount)
                    .DisposeWith(disposable);
            }
        );
    }
}

file class LeftMenuCollectionItemComparer : IComparer<ILeftMenuItemViewModel>
{
    public int Compare(ILeftMenuItemViewModel? x, ILeftMenuItemViewModel? y)
    {
        if (x is null && y is null)
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        return (x, y) switch
        {
            (CollectionLeftMenuItemViewModel a, CollectionLeftMenuItemViewModel b) => 
                a.CollectionGroupId.Value.CompareTo(b.CollectionGroupId.Value),
            ({ } a, { } b) => 
                string.Compare(a.Text.Value.Value, b.Text.Value.Value, StringComparison.Ordinal),
            _ => 0,
        };
    }
}
