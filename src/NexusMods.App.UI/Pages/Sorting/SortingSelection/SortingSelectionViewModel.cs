using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Loadouts;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class SortingSelectionViewModel : AViewModel<ISortingSelectionViewModel>, ISortingSelectionViewModel
{
    private readonly IServiceProvider _serviceProvider;

    public ReadOnlyObservableCollection<IViewModelInterface> RulesViewModels { get; }
    private readonly ObservableCollection<IViewModelInterface> _rulesViewModels = [];
    private HashSet<SortOrderVarietyId> _managedVarieties = [];
    
    private readonly BindableReactiveProperty<bool> _canEdit = new (true);
    public IReadOnlyBindableReactiveProperty<bool> CanEdit => _canEdit;
    
    public ReactiveCommand<NavigationInformation> OpenAllModsLoadoutPageCommand { get; }
    
    public SortingSelectionViewModel(
        IServiceProvider serviceProvider, 
        IWindowManager windowManager, 
        LoadoutId loadoutId, 
        Optional<Observable<bool>> canEditObservable)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        _serviceProvider = serviceProvider;
        
        var loadout = Loadout.Load(connection.Db, loadoutId);
        var game = loadout.InstallationInstance.GetGame();
        var sortingManager = game.SortOrderManager;
        var sortOrderVarieties = sortingManager.GetSortOrderVarieties();

        // Initialize with current active sort orders
        InitializeSortOrderViewModels(sortOrderVarieties, loadoutId);
        
        // Add FileConflictsViewModel
        _rulesViewModels.Add(new FileConflictsViewModel(serviceProvider, windowManager, loadoutId));
        
        RulesViewModels = new ReadOnlyObservableCollection<IViewModelInterface>(_rulesViewModels);
        
        OpenAllModsLoadoutPageCommand = new ReactiveCommand<NavigationInformation>(info =>
        {
            var pageData = new PageData
            {
                FactoryId = LoadoutPageFactory.StaticId,
                Context = new LoadoutPageContext()
                {
                    LoadoutId = loadoutId,
                    GroupScope = Optional<CollectionGroupId>.None,
                    SelectedSubTab = LoadoutPageSubTabs.Rules,
                },
            };
            var workspaceController = windowManager.ActiveWorkspaceController;
            var behavior = workspaceController.GetOpenPageBehavior(pageData, info);
            workspaceController.OpenPage(workspaceController.ActiveWorkspaceId, pageData, behavior);
        });
        
        this.WhenActivated(d =>
        {
            // Update sort orders if new ones become available
            UpdateActiveSortOrders(sortOrderVarieties, loadoutId);
            
            if (canEditObservable.HasValue)
            {
                canEditObservable.Value
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(x => _canEdit.Value = x)
                    .DisposeWith(d);
            }
        });
    }

    private void InitializeSortOrderViewModels(IReadOnlyCollection<ISortOrderVariety> sortOrderVarieties, LoadoutId loadoutId)
    {
        var varietiesWithSortOrders = sortOrderVarieties
            .Where(variety => variety.GetSortOrderIdFor(loadoutId).HasValue)
            .ToArray();

        foreach (var variety in varietiesWithSortOrders)
        {
            var viewModel = new LoadOrderViewModel(_serviceProvider, variety, loadoutId);
            _rulesViewModels.Add(viewModel);
            _managedVarieties.Add(variety.SortOrderVarietyId);
        }
    }

    private void UpdateActiveSortOrders(IReadOnlyCollection<ISortOrderVariety> sortOrderVarieties, LoadoutId loadoutId)
    {
        var varietiesWithSortOrder = sortOrderVarieties
            .Where(variety => variety.GetSortOrderIdFor(loadoutId).HasValue)
            .Select(variety => variety.SortOrderVarietyId)
            .ToHashSet();
        
        if (varietiesWithSortOrder.Count == _managedVarieties.Count)
            return;

        // Add missing sort orders
        var missingVarietyIds = varietiesWithSortOrder.Except(_managedVarieties);
        
        foreach (var varietyId in missingVarietyIds)
        {
            var variety = sortOrderVarieties.First(v => v.SortOrderVarietyId == varietyId);
            var viewModel = new LoadOrderViewModel(_serviceProvider, variety, loadoutId);
            
            // Insert before FileConflictsViewModel (which should be last)
            var insertIndex = _rulesViewModels.Count - 1;
            _rulesViewModels.Insert(insertIndex, viewModel);
            _managedVarieties.Add(varietyId);
        }
    }
}
