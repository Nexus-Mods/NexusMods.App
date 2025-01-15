using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Overlays;
using NexusMods.App.UI.Pages.Diagnostics;
using NexusMods.App.UI.Pages.LibraryPage;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LoadoutLeftMenuViewModel : AViewModel<ILoadoutLeftMenuViewModel>, ILoadoutLeftMenuViewModel
{
    public IApplyControlViewModel ApplyControlViewModel { get; }

    public ILeftMenuItemViewModel LeftMenuItemLibrary { get; }
    public ILeftMenuItemViewModel LeftMenuItemLoadout { get; }
    public ILeftMenuItemViewModel LeftMenuItemHealthCheck { get; }

    private ReadOnlyObservableCollection<ILeftMenuItemViewModel> _leftMenuCollectionItems = new([]);
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> LeftMenuCollectionItems => _leftMenuCollectionItems;
    public WorkspaceId WorkspaceId { get; }

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
        var collectionItemComparer = new LeftMenuCollectionItemComparer();
        
        LeftMenuItemLibrary = new LeftMenuItemViewModel(
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
            Text = Language.LibraryPageTitle,
            Icon = IconValues.LibraryOutline,
        };

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
            Text = Language.LoadoutView_Title_Installed_Mods,
            Icon = IconValues.Mods,
        };
        
        var collectionItemsObservable = CollectionGroup.ObserveAll(conn)
            .Filter(f => f.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutContext.LoadoutId)
            .SortBy(item => item.IsReadOnly)
            .Transform(collection => new CollectionLeftMenuItemViewModel(
                    workspaceController,
                    WorkspaceId,
                    new PageData
                    {
                        FactoryId = LoadoutPageFactory.StaticId,
                        Context = new LoadoutPageContext
                        {
                            LoadoutId = collection.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId,
                            GroupScope = collection.AsLoadoutItemGroup().LoadoutItemGroupId,
                        },
                    },
                    serviceProvider,
                    collection.CollectionGroupId
                )
                {
                    Text = collection.AsLoadoutItemGroup().AsLoadoutItem().Name,
                    Icon = IconValues.CollectionsOutline,
                }
            )
            .Transform(ILeftMenuItemViewModel (item) => item);

        LeftMenuItemHealthCheck = new LeftMenuItemViewModel(
            workspaceController,
            WorkspaceId,
            new PageData
            {
                FactoryId = DiagnosticListPageFactory.StaticId,
                Context = new DiagnosticListPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                },
            }
        )
        {
            Text = Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics,
            Icon = IconValues.Cardiology,
        };

        ApplyControlViewModel = new ApplyControlViewModel(loadoutContext.LoadoutId,
            serviceProvider,
            monitor,
            overlayController,
            gameRunningTracker
        );

        this.WhenActivated(disposable =>
            {
                collectionItemsObservable
                    .OnUI()
                    .SortAndBind(out _leftMenuCollectionItems, collectionItemComparer)
                    .Subscribe()
                    .DisposeWith(disposable);

                // diagnosticManager
                //     .CountDiagnostics(loadoutContext.LoadoutId)
                //     .OnUI()
                //     .Select(counts =>
                //         {
                //             var badges = new List<string>(capacity: 3);
                //             if (counts.NumCritical != 0)
                //                 badges.Add(counts.NumCritical.ToString());
                //             if (counts.NumWarnings != 0)
                //                 badges.Add(counts.NumWarnings.ToString());
                //             if (counts.NumSuggestions != 0)
                //                 badges.Add(counts.NumSuggestions.ToString());
                //             return badges.ToArray();
                //         }
                //     )
                //     .BindToVM(LeftMenuItemHealthCheck, vm => vm.Badges)
                //     .DisposeWith(disposable);

                LibraryUserFilters.ObserveFilteredLibraryItems(connection: conn)
                    .RemoveKey()
                    .OnUI()
                    .WhereReasonsAre(ListChangeReason.Add,
                        ListChangeReason.AddRange,
                        ListChangeReason.Remove,
                        ListChangeReason.RemoveRange
                    )
                    .SubscribeWithErrorLogging(changeSet => NewDownloadModelCount = Math.Max(0, NewDownloadModelCount + (changeSet.Adds - changeSet.Removes)))
                    .DisposeWith(disposable);

                // NOTE(erri120): No new downloads when the Left Menu gets loaded. Must be set here because the observable stream
                // above will count all existing downloads, which we want to ignore.
                NewDownloadModelCount = 0;

                // this.WhenAnyValue(vm => vm.NewDownloadModelCount)
                //     .Select(count => count == 0 ? [] : new[] { count.ToString() })
                //     .BindToVM(LeftMenuItemLibrary, vm => vm.Badges)
                //     .DisposeWith(disposable);
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
            (CollectionLeftMenuItemViewModel a, CollectionLeftMenuItemViewModel b) => a.CollectionGroupId.Value.CompareTo(b.CollectionGroupId.Value),
            ({ } a, { } b) => string.Compare(a.Text, b.Text, StringComparison.Ordinal),
            _ => 0,
        };
    }
}
