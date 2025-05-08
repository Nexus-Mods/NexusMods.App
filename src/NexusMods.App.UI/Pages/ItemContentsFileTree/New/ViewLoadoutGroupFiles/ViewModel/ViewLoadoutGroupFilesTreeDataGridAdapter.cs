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
            ColumnCreator.Create<GamePath, SharedColumns.ItemSize>(sortDirection: ListSortDirection.Descending),
            ColumnCreator.Create<GamePath, SharedColumns.FileCount>(),
        ];
    }
}

/// <summary>
/// A custom folder model initializer that adds the following components to:
/// - Track combined file size
/// - Track combined file counts
/// - Within the folder
/// </summary>
public class LoadoutGroupFilesTreeFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
{
    /// <inheritdoc/>
    public static void InitializeModel<TFolderModelInitializer>(
        CompositeItemModel<GamePath> model,
        GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
        AddNameAndIcon(model, folder);
        AddCombinedFileSize(model, folder);
        AddInnerFileCount(model, folder);
    }
    private static void AddNameAndIcon<TFolderModelInitializer>(CompositeItemModel<GamePath> model, GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder) where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
        model.Add(SharedColumns.NameWithFileIcon.StringComponentKey, new StringComponent(initialValue: folder.FolderName.ToString(), valueObservable: Observable.Return(folder.FolderName.ToString())));

        // Add the icon for the folder, making it flip on 'IsExpanded'.
        var iconStream = model
            .WhenAnyValue(m => m.IsExpanded)                    // ReactiveUI extension on IReactiveObject
            .Select(exp => exp
                ? IconValues.FolderOpen
                : IconValues.Folder);

        // hand it off to your icon‐component
        model.Add(
            SharedColumns.NameWithFileIcon.IconComponentKey,
            new UnifiedIconComponent(
                initialValue: IconValues.Folder,
                valueObservable: iconStream,
                subscribeWhenCreated: true // start observing right away
            )
        );
    }

    private static void AddCombinedFileSize<TFolderModelInitializer>(CompositeItemModel<GamePath> model, GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder) where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
        // Create an observable that transforms the file items to their sizes then sums them
        var fileSizeObservable = folder.GetAllFilesRecursiveObservable()
            .Transform(fileModel => fileModel.TryGet<SizeComponent>(SharedColumns.ItemSize.ComponentKey, out var sizeComponent) ? (long)sizeComponent.Value.Value.Value : 0L)
            .Sum(x => x) // Note(sewer): dynamicdata summation lacks unsigned. But we're talking 64-bit, good luck reaching >8 exabytes on a mod.
            .Select(x => Size.From((ulong)x)); // Sum up all the sizes
        
        // Add a ValueComponent that will update automatically when the observed total size changes
        var component = new SizeComponent(
            initialValue: Size.Zero,
            valueObservable: fileSizeObservable,
            subscribeWhenCreated: true
        );
        model.Add(SharedColumns.ItemSize.ComponentKey, component);
    }
    
    private static void AddInnerFileCount<TFolderModelInitializer>(CompositeItemModel<GamePath> model, GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder) where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
        var fileCountObservable = folder.GetAllFilesRecursiveObservable()
            .Count() // Note(sewer): This is DynamicData's Count. Not Reactive's !!
            .Select(x => (uint)x);

        var component = new UInt32Component(
            initialValue: 0,
            valueObservable: fileCountObservable,
            subscribeWhenCreated: true
        );
        model.Add(SharedColumns.FileCount.ComponentKey, component);
    }
}
