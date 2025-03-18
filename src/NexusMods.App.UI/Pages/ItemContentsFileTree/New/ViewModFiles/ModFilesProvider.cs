using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using DynamicData;
using DynamicData.Aggregation;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions.DatomIterators;
using NexusMods.MnemonicDB.Abstractions.Query;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewModFiles;

/// <summary>
/// Provides files for <see cref="LoadoutItemGroup"/>
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

    private IObservable<IChangeSet<LoadoutItem.ReadOnly, EntityId>> FilterModFiles(ModFilesFilter filesFilter)
    {
        return LoadoutItem
            .ObserveAll(_connection)
            .FilterOnObservable((_, entityId) => _connection
                .ObserveDatoms(LoadoutItem.Parent, entityId)
                .AsEntityIds()
                .FilterInModFiles(_connection, filesFilter)
                .IsNotEmpty()
            );
    }

    /// <summary>
    /// Listens to all available mod files within MnemonicDB.
    /// </summary>
    public IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> ObserveModFiles(ModFilesFilter filesFilter)
    {
        return FilterModFiles(filesFilter)
              .Transform(modFile => ToModFileItemModel(filesFilter, modFile));
    }

    private CompositeItemModel<EntityId> ToModFileItemModel(ModFilesFilter filesFilter, LoadoutItem.ReadOnly modFile)
    {
        var directChildrenObservable = _connection
            .ObserveDatoms(LoadoutItem.Parent, modFile)
            .AsEntityIds()
            .FilterInModFiles(_connection, filesFilter);

        throw new NotImplementedException();
        // Note(sewer): This seems horribly inefficient, because we visit the same children
        // over and over again, since we calculate. And filtering items also requires loading
        // all the parents.
        /*
        var allChildrenObservable = _connection
            .ObserveDatoms(LoadoutItem.Parent, modFile)
            .AsEntityIds()
            .FilterInModFiles(_connection, filesFilter)
            .Transform(childId => LoadoutItem.Load(_connection.Db, childId));
        */


        // Add row with name and icon.
        // Add row with combined size
        // Add row with file count
        // Add the file count.
        /*
        var dateObservable = childFilesObservable
            .QueryWhenChanged(query => query.Items
                .Select(static item => item.GetCreatedAt())
                .OptionalMinBy(item => item)
                .ValueOr(DateTimeOffset.MinValue)
            );

        parentItemModel.Add(SharedColumns.InstalledDate.ComponentKey, new DateComponent(
            initialValue: initialValue,
            valueObservable: dateObservable
        ));
        */
    }
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
            // TODO: Direct GET on LoadoutItem.ParentId to avoid unnecessary DB fetches.
            var item = LoadoutItem.Load(connection.Db, datom.E);
            return item.ParentId.Equals(modFilesFilter.ModId);
        });
    }
}

/// <summary>
/// A filter for filtering which mod files are shown by the <see cref="ModFilesProvider"/>
/// </summary>
/// <param name="ModId">
/// ID of the <see cref="LoadoutItemGroup"/> for the mods to which the view
/// should be filtered to.
/// </param>
public record struct ModFilesFilter(LoadoutItemGroupId ModId);
