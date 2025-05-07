using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator.Helpers;

/// <summary>
/// A <see cref="TreeFolderGeneratorCompositeItemModelAdapter{TTreeItemWithPath,TTreeItemWithPathFactory,TKey,TFolderModelInitializer}"/>
/// for storing LoadoutItem(s) (<see cref="LoadoutItemTreeItemWithPath"/>) 
/// </summary>
/// <typeparam name="TFolderModelInitializer"></typeparam>
public class TreeFolderGeneratorLoadoutTreeItemAdapter<TFolderModelInitializer> : TreeFolderGeneratorCompositeItemModelAdapter
    <
        LoadoutItemTreeItemWithPath, // Item
        LoadoutItemTreeItemWithPathFactory, // Factory (supplied by this type)
        EntityId, // We make LoadoutItemTreeItemWithPath from EntityId
        TFolderModelInitializer // Column info.
    > where TFolderModelInitializer : IFolderModelInitializer<LoadoutItemTreeItemWithPath>
{
    /// <inheritdoc />
    public TreeFolderGeneratorLoadoutTreeItemAdapter(IConnection connection, IObservable<IChangeSet<CompositeItemModel<EntityId>, EntityId>> changes) : base(new LoadoutItemTreeItemWithPathFactory(connection), changes) { }
}
