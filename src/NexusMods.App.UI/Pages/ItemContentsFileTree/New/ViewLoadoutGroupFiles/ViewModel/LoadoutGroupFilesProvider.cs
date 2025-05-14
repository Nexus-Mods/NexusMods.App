using System.Reactive.Linq;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.App.UI.Helpers;
using NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.Paths;
using ZLinq;
using static NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel.PathHelpers;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel;

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

    private IObservable<IChangeSet<LoadoutFile.ReadOnly, GamePath>> FilteredModFiles(ModFilesFilter filesFilter)
    {
        return LoadoutFile
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
            .Transform(x => ToModFileItemModel(new LoadoutFile.ReadOnly(x.Db, x.EntitySegment, x.Id), useFullFilePaths));

        // If we are requesting a flat view, we can skip folder generation.
        if (useFullFilePaths)
            return filesObservable;

        // Otherwise make all the folders via adapter.
        var adapter = new TreeFolderGeneratorLoadoutTreeItemAdapter<LoadoutGroupFilesTreeFolderModelInitializer>(filesObservable);
        var wrapper = new DisposableObservableWrapper<IChangeSet<CompositeItemModel<GamePath>, GamePath>>
            (adapter.FolderGenerator.SimplifiedObservableRoots(), adapter);
        return wrapper; // Use `SimplifiedObservableRoots` to match previous behaviour pre-CompositeItemModels.
    }

    private CompositeItemModel<GamePath> ToModFileItemModel(LoadoutFile.ReadOnly modFile, bool useFullFilePaths)
    {
        // Files don't have children.
        // We inject the relevant folders at the listener level, i.e. whatever calls `ObserveModFiles`
        var fileItemModel = new CompositeItemModel<GamePath>(FileToGamePath(modFile))
        {
            HasChildrenObservable = Observable.Return(false),
            ChildrenObservable = Observable.Empty<IChangeSet<CompositeItemModel<GamePath>, GamePath>>(),
        };
    
        // Observe changes. 
        // Note(sewer): This could maybe? be more efficient, possibly, by filtering only on attribute
        //              which we're looking for. However, at the same time, that is overhead.
        //              And we check if a value is the same as before when we assign the inner
        //              BindableReactiveProperty from the component, so actually, not filtering
        //              might be better. Food for thought.
        var itemUpdates = LoadoutFile.Observe(_connection, modFile.Id);
        var nameUpdates = itemUpdates.Select(x => useFullFilePaths ? FileToFilePath(x) : FileToFileName(x));
        var iconUpdates = itemUpdates.Select(FileToIconValue);
        var sizeUpdates = itemUpdates.Select(x => x.Size);
        
        fileItemModel.Add(SharedColumns.NameWithFileIcon.StringComponentKey, new StringComponent(initialValue: FileToFileName(modFile), valueObservable: nameUpdates));
        fileItemModel.Add(SharedColumns.NameWithFileIcon.IconComponentKey, new UnifiedIconComponent(initialValue: FileToIconValue(modFile), valueObservable: iconUpdates));
        fileItemModel.Add(SharedColumns.ItemSizeOverGamePath.ComponentKey, new SizeComponent(initialValue: modFile.Size, valueObservable: sizeUpdates));
        // Note(sewer): File Count omitted to avoid rendering a '1' for every file for cleanliness.
        //              Will see how this goes once the columns are actually there.

        return fileItemModel;
    }
}

internal static class PathHelpers
{
    internal static GamePath FileToGamePath(LoadoutFile.ReadOnly modFile)
    {
        var path = modFile.AsLoadoutItemWithTargetPath().TargetPath;
        return new GamePath(path.Item2, path.Item3);
    }
    internal static string FileToFilePath(LoadoutFile.ReadOnly modFile) => modFile.AsLoadoutItemWithTargetPath().TargetPath.Item3;
    internal static IconValue FileToIconValue(LoadoutFile.ReadOnly modFile) => ((RelativePath)FileToFileName(modFile)).Extension.GetIconType().GetIconValue();
    internal static string FileToFileName(LoadoutFile.ReadOnly modFile) => ((RelativePath)FileToFilePath(modFile)).FileName;
}

internal static class LoadoutFilesObservableExtensions
{
    internal static IObservable<IChangeSet<Datom, GamePath>> FilterInModFiles(
        this IObservable<IChangeSet<Datom, EntityId>> source,
        IConnection connection,
        ModFilesFilter modFilesFilter)
    {
        return source.ChangeKey(x => FileToGamePath(new LoadoutFile.ReadOnly(connection.Db, x.E)))
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
