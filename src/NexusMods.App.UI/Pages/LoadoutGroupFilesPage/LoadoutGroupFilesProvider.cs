using System.Reactive.Linq;
using DynamicData;
using DynamicData.Aggregation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.UI.Sdk.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using R3;
using ZLinq;
using static NexusMods.App.UI.Pages.LoadoutGroupFilesPage.PathHelpers;

namespace NexusMods.App.UI.Pages.LoadoutGroupFilesPage;

/// <summary>
/// Provides files for multiple <see cref="Abstractions.Loadouts.LoadoutItemGroup"/>(s) specified by a <see cref="ModFilesFilter"/>.
/// </summary>
[UsedImplicitly]
public class LoadoutGroupFilesProvider
{
    private readonly IConnection _connection;

    /// <summary/>
    public LoadoutGroupFilesProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    private IObservable<IChangeSet<LoadoutItemWithTargetPath.ReadOnly, GamePath>> FilteredModFiles(ModFilesFilter filesFilter)
    {
        return LoadoutItemWithTargetPath
            .ObserveAll(_connection)
            .Filter(x => LoadoutFilesObservableExtensions.FilterEntityId(_connection, filesFilter, x.Id))
            .ChangeKey(FileToGamePath);
    }

    /// <summary>
    /// Listens to all available mod files within MnemonicDB.
    /// </summary>
    /// <param name="filesFilter">A filter which specifies one or more mod groups of items to display.</param>
    /// <param name="useFullFilePaths">Renders the file names as full file paths, for when the data is viewed outside a tree.</param>
    public IObservable<IChangeSet<CompositeItemModel<GamePath>, GamePath>> ObserveModFiles(ModFilesFilter filesFilter, bool useFullFilePaths)
    {
        var filesObservable = FilteredModFiles(filesFilter)
            .Transform(x => ToModFileItemModel(new LoadoutItemWithTargetPath.ReadOnly(x.Db, x.EntitySegment, x.Id), useFullFilePaths));

        // If we are requesting a flat view, we can skip folder generation.
        if (useFullFilePaths)
            return filesObservable;

        // Otherwise make all the folders via adapter.
        var adapter = new TreeFolderGeneratorLoadoutTreeItemAdapter<LoadoutGroupFilesTreeFolderModelInitializer>(filesObservable);
        var wrapper = new DisposableObservableWrapper<IChangeSet<CompositeItemModel<GamePath>, GamePath>>
            (adapter.FolderGenerator.ObservableRoots(), adapter);
        return wrapper; // Use `SimplifiedObservableRoots` to match previous behaviour pre-CompositeItemModels.
    }

    private CompositeItemModel<GamePath> ToModFileItemModel(LoadoutItemWithTargetPath.ReadOnly modFile, bool useFullFilePaths)
    {
        // Files don't have children.
        // We inject the relevant folders at the listener level, i.e. whatever calls `ObserveModFiles`
        var fileItemModel = new CompositeItemModel<GamePath>(FileToGamePath(modFile))
        {
            HasChildrenObservable = System.Reactive.Linq.Observable.Return(false),
            ChildrenObservable = System.Reactive.Linq.Observable.Empty<IChangeSet<CompositeItemModel<GamePath>, GamePath>>(),
        };
    
        // Observe changes. 
        // Note(sewer): This could maybe? be more efficient, possibly, by filtering only on attribute
        //              which we're looking for. However, at the same time, that is overhead.
        //              And we check if a value is the same as before when we assign the inner
        //              BindableReactiveProperty from the component, so actually, not filtering
        //              might be better. Food for thought.
        var itemUpdates = LoadoutItemWithTargetPath.Observe(_connection, modFile.Id);
        var nameUpdates = itemUpdates.Select(x => useFullFilePaths ? FileToFilePath(x) : FileToFileName(x));
        var iconUpdates = itemUpdates.Select(FileToIconValue);
        var sizeUpdates = itemUpdates.Select(x => LoadoutFile.Size.TryGetValue(x, out var sizeVal) ? sizeVal : Size.Zero);

        fileItemModel.Add(SharedColumns.NameWithFileIcon.FileEntryComponentKey,
            new FileEntryComponent(
                new StringComponent(initialValue: FileToFileName(modFile), valueObservable: nameUpdates),
                new ValueComponent<bool>(modFile.IsDeletedFile())
            ));
        fileItemModel.Add(SharedColumns.NameWithFileIcon.IconComponentKey, new UnifiedIconComponent(initialValue: FileToIconValue(modFile), valueObservable: iconUpdates));
        fileItemModel.Add(SharedColumns.ItemSizeOverGamePath.ComponentKey, new SizeComponent(initialValue: LoadoutFile.Size.TryGetValue(modFile, out var size) ? size : Size.Zero, valueObservable: sizeUpdates));
        // Note(sewer): File Count omitted to avoid rendering a '1' for every file for cleanliness.
        //              Will see how this goes once the columns are actually there.
        
        return fileItemModel;
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

    private static void AddNameAndIcon<TFolderModelInitializer>(
        CompositeItemModel<GamePath> model,
        GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder)
        where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
       // Add name
        model.Add(SharedColumns.NameWithFileIcon.FileEntryComponentKey, 
            new FileEntryComponent(new StringComponent(folder.DisplayName), isDeleted: new ValueComponent<bool>(false)));

        // Add the icon for the folder, making it flip on 'IsExpanded'.
        var iconStream = model.ObservePropertyChanged(m => m.IsExpanded)
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

    private static void AddCombinedFileSize<TFolderModelInitializer>(
        CompositeItemModel<GamePath> model, 
        GeneratedFolder<GamePathTreeItemWithPath, TFolderModelInitializer> folder) 
        where TFolderModelInitializer : IFolderModelInitializer<GamePathTreeItemWithPath>
    {
        // Create an observable that transforms the file items to their sizes then sums them
        var fileSizeObservable = folder.GetAllFilesRecursiveObservable()
            .Transform(fileModel => fileModel.TryGet<SizeComponent>(SharedColumns.ItemSizeOverGamePath.ComponentKey, out var sizeComponent) ? (long)sizeComponent.Value.Value.Value : 0L)
            .Sum(x => x) // Note(sewer): dynamicdata summation lacks unsigned. But we're talking 64-bit, good luck reaching >8 exabytes on a mod.
            .Select(x => Size.From((ulong)x)); // Sum up all the sizes
        
        // Add a ValueComponent that will update automatically when the observed total size changes
        var component = new SizeComponent(
            initialValue: Size.Zero,
            valueObservable: fileSizeObservable,
            subscribeWhenCreated: true
        );
        model.Add(SharedColumns.ItemSizeOverGamePath.ComponentKey, component);
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

internal static class PathHelpers
{
    internal static GamePath FileToGamePath(LoadoutItemWithTargetPath.ReadOnly modFile)
    {
        var path = modFile.TargetPath;
        return new GamePath(path.Item2, path.Item3);
    }
    internal static string FileToFilePath(LoadoutItemWithTargetPath.ReadOnly modFile) => modFile.TargetPath.Item3;
    internal static IconValue FileToIconValue(LoadoutItemWithTargetPath.ReadOnly modFile) => ((RelativePath)FileToFileName(modFile)).Extension.GetIconType().GetIconValue();
    internal static string FileToFileName(LoadoutItemWithTargetPath.ReadOnly modFile) => ((RelativePath)FileToFilePath(modFile)).FileName;
}

internal static class LoadoutFilesObservableExtensions
{
    internal static IObservable<IChangeSet<Datom, GamePath>> FilterInModFiles(
        this IObservable<IChangeSet<Datom, EntityId>> source,
        IConnection connection,
        ModFilesFilter modFilesFilter)
    {
        return source.ChangeKey(x => FileToGamePath(new LoadoutItemWithTargetPath.ReadOnly(connection.Db, x.E)))
              .Filter(datom => FilterEntityId(connection, modFilesFilter, datom.E));
    }

    internal static bool FilterEntityId(IConnection connection, ModFilesFilter modFilesFilter, EntityId eId)
    {
        // Note(sewer): Direct GET on LoadoutItem.ParentId to avoid unnecessary boxing or DB fetches.
        var segment = connection.Db.Get(eId);

        // Note(Al12rs): This only checks the direct parent of the LoadoutItem, not higher anchestors
        var hasParent = LoadoutItem.Parent.TryGetValue(segment, out var parentId);
        if (!hasParent)
            return false;

        var matchesAnyId = modFilesFilter.ModIds
            .AsValueEnumerable()
            .Any(filter => parentId.Equals(filter.Value));
        return matchesAnyId;
    }
}

/// <summary>
/// A filter for filtering which mod files are shown by the <see cref="LoadoutGroupFilesProvider"/>
/// </summary>
/// <param name="ModIds">
/// IDs of the <see cref="Abstractions.Loadouts.LoadoutItemGroup"/> for the mods to which the view
/// should be filtered to.
/// </param>
public record struct ModFilesFilter(LoadoutItemGroupId[] ModIds);
