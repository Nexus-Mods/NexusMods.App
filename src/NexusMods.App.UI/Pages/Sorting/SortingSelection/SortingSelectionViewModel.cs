using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LoadoutPage;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class SortingSelectionViewModel : AViewModel<ISortingSelectionViewModel>, ISortingSelectionViewModel
{
    private readonly LoadoutId _loadoutId;
    private readonly IConnection _connection;
    public IViewModelInterface[] ViewModels { get; }
    private readonly BindableReactiveProperty<bool> _canEdit = new (true);
    public IReadOnlyBindableReactiveProperty<bool> CanEdit => _canEdit;
    
    public ReactiveCommand<NavigationInformation> OpenAllModsLoadoutPageCommand { get; }
    
    public SortingSelectionViewModel(IServiceProvider serviceProvider, IWindowManager windowManager, LoadoutId loadoutId, Optional<Observable<bool>> canEditObservable)
    {
        _loadoutId = loadoutId;
        _connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(_connection.Db, _loadoutId);
        var sortableItemProviders = loadout
            .InstallationInstance
            .GetGame()
            .SortableItemProviderFactories;

        var viewModels = sortableItemProviders
            .Select(IViewModelInterface (providerFactory) => new LoadOrderViewModel(serviceProvider, providerFactory, providerFactory.GetLoadoutSortableItemProvider(loadout)))
            .ToList();

        viewModels.Add(new FileConflictsViewModel(serviceProvider, loadoutId));
        ViewModels = viewModels.ToArray();

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
            }
        );
        
        this.WhenActivated(d =>
        {
            if (canEditObservable.HasValue)
            {
                canEditObservable.Value
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe(x => _canEdit.Value = x)
                    .DisposeWith(d);
            }
        });
    }
}
