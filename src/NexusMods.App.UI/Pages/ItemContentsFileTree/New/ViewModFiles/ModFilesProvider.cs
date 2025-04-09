using System.Reactive.Linq;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using DynamicData;
using DynamicData.Aggregation;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Trees.Common;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.Paths;
using ZLinq;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewModFiles;

/// <summary>
/// Provides files for multiple <see cref="LoadoutItemGroup"/>(s) specified by a <see cref="ModFilesFilter"/>.
/// </summary>
[UsedImplicitly]
public class ModFilesProvider
{
    private readonly IConnection _connection;

    /// <summary/>
    public ModFilesProvider(IServiceProvider serviceProvider)
    {
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    private IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> FilteredModFiles(ModFilesFilter filesFilter)
    {
        return LoadoutItem
            .ObserveAll(_connection)
            .FilterOnObservable((x, entityId) => _connection
                .ObserveDatoms(LoadoutItem.Parent, entityId)
                .AsEntityIds()
                .FilterInModFiles(_connection, filesFilter)
                .IsNotEmpty()
            );
    }

    /// <summary>
    /// Listens to all available mod files within MnemonicDB.
    /// </summary>
    /// <param name="filesFilter">A filter which specifies one or more mod groups of items to display.</param>
    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveModFiles(ModFilesFilter filesFilter)
    {
        return FilteredModFiles(filesFilter)
              .Transform(x => ToModFileItemModel(new LoadoutFile.ReadOnly(x.Db, x.EntitySegment, x.Id)));
    }

    private CompositeItemModel<EntityId> ToModFileItemModel(LoadoutFile.ReadOnly modFile)
    {
        // Files don't have children.
        // We inject the relevant folders at the listener level, i.e. whatever calls `ObserveModFiles`
        var fileItemModel = new CompositeItemModel<EntityId>(modFile.Id)
        {
            HasChildrenObservable = Observable.Return(false),
            ChildrenObservable = Observable.Empty<IChangeSet<CompositeItemModel<EntityId>, EntityId>>(),
        };
    
        // Observe changes. 
        // Note(sewer): This could maybe? be more efficient, possibly, by filtering only on attribute
        //              which we're looking for. However, at the same time, that is overhead.
        //              And we check if a value is the same as before when we assign the inner
        //              BindableReactiveProperty from the component, so actually, not filtering
        //              might be better. Food for thought.
        var itemUpdates = LoadoutFile.Observe(_connection, modFile.Id);
        var nameUpdates = itemUpdates.Select(FileToFileName);
        var iconUpdates = itemUpdates.Select(FileToIconValue);
        var sizeUpdates = itemUpdates.Select(x => x.Size);
        
        fileItemModel.Add(Columns.NameWithFileIcon.StringComponentKey, new StringComponent(initialValue: FileToFileName(modFile), valueObservable: nameUpdates));
        fileItemModel.Add(Columns.NameWithFileIcon.IconComponentKey, new UnifiedIconComponent(initialValue: FileToIconValue(modFile), valueObservable: iconUpdates));
        fileItemModel.Add(SharedColumns.ItemSize.ComponentKey, new SizeComponent(initialValue: modFile.Size, valueObservable: sizeUpdates));
        // Note(sewer): File Count omitted to avoid rendering a '1' for every file for cleanliness.
        //              Will see how this goes once the columns are actually there.

        return fileItemModel;
    }

    private static IconValue FileToIconValue(LoadoutFile.ReadOnly modFile) => ((RelativePath)FileToFileName(modFile)).Extension.GetIconType().GetIconValue();
    private static string FileToFileName(LoadoutFile.ReadOnly modFile) => modFile.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName;
}

internal static class ModFilesObservableExtensions
{
    internal static IObservable<IChangeSet<Datom, EntityId>> FilterInModFiles(
        this IObservable<IChangeSet<Datom, EntityId>> source,
        IConnection connection,
        ModFilesFilter modFilesFilter)
    {
        return source.Filter(datom =>
        {
            var segment = connection.Db.Get(datom.E);
            
            // Assert that this is a LoadoutFile, in case the group contains non-Files.
            var loadoutFile = new LoadoutFile.ReadOnly(connection.Db, segment, datom.E);
            if (!loadoutFile.IsValid())
                return false;
            
            // Note(sewer): Direct GET on LoadoutItem.ParentId to avoid unnecessary boxing or DB fetches.
            var hasParent = LoadoutItem.Parent.TryGetValue(segment, out var parentId);
            if (!hasParent)
                return false;
            
            return modFilesFilter.ModIds
                .AsValueEnumerable()
                .Any(filter => parentId.Equals(filter.Value));
        });
    }
}

/// <summary>
/// A filter for filtering which mod files are shown by the <see cref="ModFilesProvider"/>
/// </summary>
/// <param name="ModIds">
/// IDs of the <see cref="LoadoutItemGroup"/> for the mods to which the view
/// should be filtered to.
/// </param>
public record struct ModFilesFilter(LoadoutItemGroupId[] ModIds);
