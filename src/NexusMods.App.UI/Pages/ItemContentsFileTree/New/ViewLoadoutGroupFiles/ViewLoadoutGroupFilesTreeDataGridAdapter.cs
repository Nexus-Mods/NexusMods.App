using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles;

/// <summary>
/// Implementation of <see cref="TreeDataGridAdapter{TModel,TKey}"/> 
/// </summary>
public class ViewLoadoutGroupFilesTreeDataGridAdapter(IServiceProvider serviceProvider, ModFilesFilter filesFilter) : TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>
{
    private readonly LoadoutGroupFilesProvider _loadoutGroupFilesProvider = serviceProvider.GetRequiredService<LoadoutGroupFilesProvider>();

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        // This view is always flat (until we add folders), so we ignore viewHierarchical and directly observe files.
        return _loadoutGroupFilesProvider.ObserveModFiles(filesFilter, viewHierarchical);
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, Columns.NameWithFileIcon>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, SharedColumns.ItemSize>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<EntityId, Columns.FileCount>(),
        ];
    }
}
