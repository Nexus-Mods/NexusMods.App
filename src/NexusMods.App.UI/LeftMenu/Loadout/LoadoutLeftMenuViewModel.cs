using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.LeftMenu.Items;
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

    private readonly SourceList<ILeftMenuItemViewModel> _items = new();
    private ReadOnlyObservableCollection<ILeftMenuItemViewModel> _finalCollection = new([]);
    
    private readonly SourceList<ILeftMenuItemViewModel> _collectionGroupItems = new();

    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items => _finalCollection;
    public WorkspaceId WorkspaceId { get; }

    [Reactive] private int NewDownloadModelCount { get; set; }

    public LoadoutLeftMenuViewModel(
        LoadoutContext loadoutContext,
        WorkspaceId workspaceId,
        IWorkspaceController workspaceController,
        IServiceProvider serviceProvider)
    {
        var diagnosticManager = serviceProvider.GetRequiredService<IDiagnosticManager>();
        var conn = serviceProvider.GetRequiredService<IConnection>();

        WorkspaceId = workspaceId;
        ApplyControlViewModel = new ApplyControlViewModel(loadoutContext.LoadoutId, serviceProvider);
        
        
        var installedModsItem = new IconViewModel
        {
            Name = Language.LoadoutView_Title_Installed_Mods,
            RelativeOrder = 1,
            Icon = IconValues.Mods,
            NavigateCommand = ReactiveCommand.Create<NavigationInformation>(info =>
            {
                var pageData = new PageData
                {
                    FactoryId = LoadoutPageFactory.StaticId,
                    Context = new LoadoutPageContext
                    {
                        LoadoutId = loadoutContext.LoadoutId,
                        GroupScope = Optional<LoadoutItemGroupId>.None,
                    },
                };
                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
            }),
        };

        
        var libraryItem = new IconViewModel
        {
            Name = Language.LibraryPageTitle,
            RelativeOrder = 3,
            Icon = IconValues.ModLibrary,
            NavigateCommand = ReactiveCommand.Create<NavigationInformation>(info =>
            {
                NewDownloadModelCount = 0;

                var pageData = new PageData
                {
                    FactoryId = LibraryPageFactory.StaticId,
                    Context = new LibraryPageContext
                    {
                        LoadoutId = loadoutContext.LoadoutId,
                    },
                };

                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
            }),
        };

        var diagnosticItem = new IconViewModel
        {
            Name = Language.LoadoutLeftMenuViewModel_LoadoutLeftMenuViewModel_Diagnostics,
            RelativeOrder = 4,
            Icon = IconValues.Stethoscope,
            NavigateCommand = ReactiveCommand.Create<NavigationInformation>(info =>
            {
                var pageData = new PageData
                {
                    FactoryId = DiagnosticListPageFactory.StaticId,
                    Context = new DiagnosticListPageContext
                    {
                        LoadoutId = loadoutContext.LoadoutId,
                    },
                };

                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
            }),
        };
        


        var tools = new ILeftMenuItemViewModel[]
        {
            installedModsItem,
            libraryItem,
            diagnosticItem,
        };

        _items.AddRange(tools);
        
        this.WhenActivated(disposable =>
        {
            _collectionGroupItems.Clear();
            CollectionGroup.ObserveAll(conn)
                .Filter(f => f.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutContext.LoadoutId)
                .SortBy(itm => itm.IsReadOnly)
                .Transform(itm => MakeLoadoutItemGroupViewModel(workspaceController, itm, serviceProvider))
                .Subscribe(s =>
                {
                    _collectionGroupItems.Edit(x => {
                        foreach (var change in s)
                        {
                            if (change.Reason == ChangeReason.Add)
                                x.Add(change.Current);
                            if (change.Reason == ChangeReason.Remove)
                                x.Remove(change.Current);
                            if (change.Reason == ChangeReason.Update)
                            {
                                x.Remove(change.Previous.Value);
                                x.Add(change.Current);
                            }
                        }
                    });
                })
                .DisposeWith(disposable);

            _items.Connect()
                .Merge(_collectionGroupItems.Connect())
                .Sort(new LeftMenuComparer())
                .Bind(out _finalCollection)
                .Subscribe()
                .DisposeWith(disposable);
                
            diagnosticManager
                .CountDiagnostics(loadoutContext.LoadoutId)
                .OnUI()
                .Select(counts =>
                {
                    var badges = new List<string>(capacity: 3);
                    if (counts.NumCritical != 0)
                        badges.Add(counts.NumCritical.ToString());
                    if (counts.NumWarnings != 0)
                        badges.Add(counts.NumWarnings.ToString());
                    if (counts.NumSuggestions != 0)
                        badges.Add(counts.NumSuggestions.ToString());
                    return badges.ToArray();
                })
                .BindToVM(diagnosticItem, vm => vm.Badges)
                .DisposeWith(disposable);

            LibraryUserFilters.ObserveFilteredLibraryItems(connection: conn)
                .RemoveKey()
                .OnUI()
                .WhereReasonsAre(ListChangeReason.Add, ListChangeReason.AddRange, ListChangeReason.Remove, ListChangeReason.RemoveRange)
                .SubscribeWithErrorLogging(changeSet => NewDownloadModelCount = Math.Max(0, NewDownloadModelCount + (changeSet.Adds - changeSet.Removes)))
                .DisposeWith(disposable);

            // NOTE(erri120): No new downloads when the Left Menu gets loaded. Must be set here because the observable stream
            // above will count all existing downloads, which we want to ignore.
            NewDownloadModelCount = 0;

            this.WhenAnyValue(vm => vm.NewDownloadModelCount)
                .Select(count => count == 0 ? [] : new[] { count.ToString() })
                .BindToVM(libraryItem, vm => vm.Badges)
                .DisposeWith(disposable);
        });
    }

    private ILeftMenuItemViewModel MakeLoadoutItemGroupViewModel(IWorkspaceController workspaceController, CollectionGroup.ReadOnly itm, IServiceProvider serviceProvider)
    {
        var vm = new LeftMenuCollectionViewModel
        {
            CollectionGroupId = itm.CollectionGroupId,
            Name = itm.AsLoadoutItemGroup().AsLoadoutItem().Name,
            Icon = IconValues.Collections,
            RelativeOrder = 2,
            NavigateCommand = ReactiveCommand.Create<NavigationInformation>(info =>
            {
                var pageData = new PageData
                {
                    FactoryId = LoadoutPageFactory.StaticId,
                    Context = new LoadoutPageContext
                    {
                        LoadoutId = itm.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId,
                        GroupScope = itm.AsLoadoutItemGroup().LoadoutItemGroupId,
                    },
                };

                var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
                workspaceController.OpenPage(WorkspaceId, pageData, behavior);
            }),
        };
        return vm;
    }
}

file class LeftMenuComparer : IComparer<ILeftMenuItemViewModel>
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
            (LeftMenuCollectionViewModel a, LeftMenuCollectionViewModel b) => a.CollectionGroupId.Value.CompareTo(b.CollectionGroupId.Value),
            (IconViewModel a, IconViewModel b) => a.RelativeOrder.CompareTo(b.RelativeOrder),
            _ => 0,
        };
    }
}
