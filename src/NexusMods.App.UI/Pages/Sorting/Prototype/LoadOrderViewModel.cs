using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting.Prototype;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    private readonly ReadOnlyObservableCollection<ISortableItemViewModel> _sortableItemViewModels;

    public string SortOrderName { get; }
    public ReadOnlyObservableCollection<ISortableItemViewModel> SortableItems => _sortableItemViewModels;

    public ITreeDataGridSource<ISortableItemViewModel> TreeSource { get; }

    public LoadOrderViewModel(LoadoutId loadoutId, ISortableItemProviderFactory sortableItemProviderFactory)
    {
        SortOrderName = sortableItemProviderFactory.SortOrderName;
        var provider = sortableItemProviderFactory.GetLoadoutSortableItemProvider(loadoutId);

        var subscription = provider
            .SortableItems
            .ToObservableChangeSet()
            .Transform(item => (ISortableItemViewModel)new SortableItemViewModel(item))
            .Bind(out _sortableItemViewModels);
        
        

        var source = new FlatTreeDataGridSource<ISortableItemViewModel>(_sortableItemViewModels)
        {
            Columns =
            {
                new TemplateColumn<ISortableItemViewModel>(
                    "Load Order",
                    "IndexColumnDataTemplate",
                    options: new TemplateColumnOptions<ISortableItemViewModel>
                    {
                        // sort by SortIndex
                        CompareAscending = (x, y) =>
                        {
                            if (x is null || y is null)
                                return 0;
                            return x.SortIndex.CompareTo(y.SortIndex);
                        },
                        CompareDescending = (x, y) =>
                        {
                            if (x is null || y is null)
                                return 0;
                            return y.SortIndex.CompareTo(x.SortIndex);
                        }
                    }
                ),
                new TextColumn<ISortableItemViewModel, string>(
                    "Name",
                    x => x.DisplayName
                ),
            },
        };
        
        var selection = new TreeDataGridRowSelectionModel<ISortableItemViewModel>(source)
        {
            SingleSelect = false,
        };
        source.Selection = selection;
        
        TreeSource = source;

        this.WhenActivated(d =>
            {
                subscription.Subscribe()
                    .DisposeWith(d);
            }
        );
    }
}
