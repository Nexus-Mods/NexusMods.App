using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// Adapter for <see cref="LoadoutItem.ReadOnly"/> for <see cref="ITreeItemWithPath"/>.
/// </summary>
public readonly struct LoadoutItemTreeItemWithPath : ITreeItemWithPath
{
    private readonly LoadoutItem.ReadOnly _item;

    /// <summary/>
    public LoadoutItemTreeItemWithPath(LoadoutItem.ReadOnly item) => _item = item;

    /// <inheritdoc />
    public GamePath GetPath()
    {
        return !_item.TryGetAsLoadoutItemWithTargetPath(out var withTargetPath) 
            ? new GamePath(LocationId.Unknown, "") 
            : withTargetPath.TargetPath;
    }

    public static implicit operator LoadoutItemTreeItemWithPath(LoadoutItem.ReadOnly item) => new(item);
    public static implicit operator LoadoutItem.ReadOnly(LoadoutItemTreeItemWithPath adapter) => adapter._item;
}

/// <summary>
/// A factory for creating <see cref="LoadoutItemTreeItemWithPath"/> from <see cref="EntityId"/>.
/// And a database connection.
/// </summary>
public readonly struct LoadoutItemTreeItemWithPathFactory(IConnection connection) : ITreeItemWithPathFactory<EntityId, LoadoutItemTreeItemWithPath>
{
    public LoadoutItemTreeItemWithPath CreateItem(EntityId key) => new LoadoutItem.ReadOnly(connection.Db, key);
}
