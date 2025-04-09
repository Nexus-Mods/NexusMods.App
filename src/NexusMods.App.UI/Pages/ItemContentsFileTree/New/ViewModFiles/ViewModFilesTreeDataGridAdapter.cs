using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewModFiles;

/// <summary>
/// Implementation of <see cref="TreeDataGridAdapter{TModel,TKey}"/> 
/// </summary>
public class ViewModFilesTreeDataGridAdapter(IServiceProvider serviceProvider, ModFilesFilter filesFilter) : TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>
{
    private readonly LoadoutFilesProvider _loadoutFilesProvider = serviceProvider.GetRequiredService<LoadoutFilesProvider>();

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        // This view is always flat, so we ignore viewHierarchical and directly observe files.
        return _loadoutFilesProvider.ObserveModFiles(filesFilter, viewHierarchical);
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, Columns.NameWithFileIcon>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
            ColumnCreator.Create<EntityId, SharedColumns.ItemSize>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<EntityId, SharedColumns.Value<uint>>(),
        ];
    }
}
