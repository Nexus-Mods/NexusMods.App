using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Aggregation;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.Icons;
using NexusMods.Paths;
using ReactiveUI;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel;

/// <summary>
/// Implementation of <see cref="TreeDataGridAdapter{TModel,TKey}"/> 
/// </summary>
public class ViewLoadoutGroupFilesTreeDataGridAdapter(IServiceProvider serviceProvider, ModFilesFilter filesFilter) : TreeDataGridAdapter<CompositeItemModel<GamePath>, GamePath>
{
    private readonly LoadoutGroupFilesProvider _loadoutGroupFilesProvider = new(serviceProvider);
    
    protected override IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> GetRootsObservable(bool viewHierarchical) => _loadoutGroupFilesProvider.ObserveModFiles(filesFilter, useFullFilePaths: !viewHierarchical);

    protected override IColumn<CompositeItemModel<GamePath>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<GamePath, SharedColumns.NameWithFileIcon>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<GamePath>, GamePath>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<GamePath, SharedColumns.ItemSizeOverGamePath>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<GamePath, SharedColumns.FileCount>(),
        ];
    }
}


