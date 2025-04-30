using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Aggregation;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.Icons;
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

/// <summary>
/// A custom folder model initializer that adds the following components to:
/// - Track combined file size
/// - Track combined file counts
/// - Within the folder
/// </summary>
public class LoadoutGroupFilesTreeFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
{
    /// <inheritdoc/>
    public static void InitializeModel<TFolderModelInitializer>(
        CompositeItemModel<EntityId> model,
        GeneratedFolder<ITreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
    {
        AddNameAndIcon(model, folder);
        AddCombinedFileSize(model, folder);
        AddInnerFileCount(model, folder);
    }
    private static void AddNameAndIcon<TFolderModelInitializer>(CompositeItemModel<EntityId> model, GeneratedFolder<ITreeItemWithPath, TFolderModelInitializer> folder) where TFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
    {
        model.Add(Columns.NameWithFileIcon.StringComponentKey, new StringComponent(initialValue: folder.FolderPath.FileName.ToString(), valueObservable: Observable.Return(folder.FolderPath.FileName.ToString())));
        
        // TODO: Wire up folder open/close events.
        model.Add(Columns.NameWithFileIcon.IconComponentKey, new UnifiedIconComponent(initialValue: IconValues.Folder, valueObservable: Observable.Return(IconValues.Folder)));
    }

    private static void AddCombinedFileSize<TFolderModelInitializer>(CompositeItemModel<EntityId> model, GeneratedFolder<ITreeItemWithPath, TFolderModelInitializer> folder) where TFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
    {
        // Create an observable that transforms the file items to their sizes then sums them
        var fileSizeObservable = folder.GetAllFilesRecursiveObservable()
            .Transform(fileModel => fileModel.TryGet<ValueComponent<long>>(SharedColumns.ItemSize.ComponentKey, out var sizeComponent) ? sizeComponent.Value.Value : 0L)
            .Sum(x => x); // Sum up all the sizes
        
        // Add a ValueComponent that will update automatically when the observed total size changes
        var component = new ValueComponent<long>(
            initialValue: 0,
            valueObservable: fileSizeObservable,
            subscribeWhenCreated: true,
            observeOutsideUiThread: true
        );
        model.Add(SharedColumns.ItemSize.ComponentKey, component);
    }
    
    private static void AddInnerFileCount<TFolderModelInitializer>(CompositeItemModel<EntityId> model, GeneratedFolder<ITreeItemWithPath, TFolderModelInitializer> folder) where TFolderModelInitializer : IFolderModelInitializer<ITreeItemWithPath>
    {
        var fileCountObservable = folder.GetAllFilesRecursiveObservable()
            .Count(); // Note(sewer): This is DynamicData's Count. Not Reactive's !!

        var component = new ValueComponent<int>(
            initialValue: 0,
            valueObservable: fileCountObservable,
            subscribeWhenCreated: true,
            observeOutsideUiThread: true
        );
        model.Add(SharedColumns.Value<uint>.ComponentKey, component);
    }
}
