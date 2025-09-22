using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Media.Imaging;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Resources;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class FileConflictsViewModel : AViewModel<IFileConflictsViewModel>, IFileConflictsViewModel
{
    public FileConflictsTreeDataGridAdapter TreeDataGridAdapter { get; }

    public FileConflictsViewModel(IServiceProvider serviceProvider, LoadoutId loadoutId)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var loadout = Loadout.Load(connection.Db, loadoutId);
        Debug.Assert(loadout.IsValid());

        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        TreeDataGridAdapter = new FileConflictsTreeDataGridAdapter(serviceProvider, connection, synchronizer, loadoutId);

        this.WhenActivated(disposables =>
        {
            TreeDataGridAdapter.Activate().AddTo(disposables);
        });
    }
}

public class FileConflictsTreeDataGridAdapter : TreeDataGridAdapter<CompositeItemModel<EntityId>, EntityId>
{
    private readonly IConnection _connection;
    private readonly ILoadoutSynchronizer _synchronizer;
    private readonly IResourceLoader<EntityId, Bitmap> _modPageThumbnailPipeline;
    private readonly LoadoutId _loadoutId;

    public FileConflictsTreeDataGridAdapter(IServiceProvider serviceProvider, IConnection connection, ILoadoutSynchronizer synchronizer, LoadoutId loadoutId)
    {
        _connection = connection;
        _synchronizer = synchronizer;
        _loadoutId = loadoutId;

        _modPageThumbnailPipeline = ImagePipelines.GetModPageThumbnailPipeline(serviceProvider);
    }

    protected override IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> GetRootsObservable(bool viewHierarchical)
    {
        var sourceCache = new SourceCache<CompositeItemModel<EntityId>, EntityId>(x => x.Key);

        var conflicts = _synchronizer.GetFileConflictsByParentGroup(Loadout.Load(_connection.Db, _loadoutId));
        var enumerable = conflicts.Select(ToItemModel);
        sourceCache.AddOrUpdate(enumerable);

        return sourceCache.Connect();
    }

    private CompositeItemModel<EntityId> ToItemModel(KeyValuePair<LoadoutItemGroup.ReadOnly, LoadoutFile.ReadOnly[]> kv)
    {
        var (loadoutGroup, loadoutFiles) = kv;
        var itemModel = new CompositeItemModel<EntityId>(loadoutGroup.Id);

        itemModel.Add(SharedColumns.Name.NameComponentKey, new NameComponent(value: loadoutGroup.AsLoadoutItem().Name));
        ImageComponent? imageComponent = null;

        if (loadoutGroup.TryGetAsLibraryLinkedLoadoutItem(out var libraryLinkedLoadoutItem))
        {
            if (libraryLinkedLoadoutItem.LibraryItem.TryGetAsNexusModsLibraryItem(out var nexusLibraryItem))
            {
                imageComponent = ImageComponent.FromPipeline(_modPageThumbnailPipeline, nexusLibraryItem.ModPageMetadataId, ImagePipelines.ModPageThumbnailFallback);
            }
        }

        imageComponent ??= new ImageComponent(value: ImagePipelines.ModPageThumbnailFallback);
        itemModel.Add(SharedColumns.Name.ImageComponentKey, imageComponent);

        return itemModel;
    }

    protected override IColumn<CompositeItemModel<EntityId>>[] CreateColumns(bool viewHierarchical)
    {
        var nameColumn = ColumnCreator.Create<EntityId, SharedColumns.Name>();

        return
        [
            viewHierarchical ? ITreeDataGridItemModel<CompositeItemModel<EntityId>, EntityId>.CreateExpanderColumn(nameColumn) : nameColumn,
        ];
    }
}
