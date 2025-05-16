using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// Adapter for <see cref="LoadoutItem.ReadOnly"/> for <see cref="ITreeItemWithPath"/>.
/// </summary>
public readonly struct GamePathTreeItemWithPath : ITreeItemWithPath
{
    private readonly GamePath _path;

    /// <summary/>
    public GamePathTreeItemWithPath(GamePath item) => _path = item;

    /// <inheritdoc />
    public GamePath GetPath() => _path;
    
    /// <summary/>
    public static implicit operator GamePathTreeItemWithPath(GamePath path) => new(path);
}

/// <summary>
/// A factory for creating <see cref="GamePathTreeItemWithPath"/> from <see cref="EntityId"/>.
/// And a database connection.
/// </summary>
public readonly struct GamePathTreeItemWithPathFactory : ITreeItemWithPathFactory<GamePath, GamePathTreeItemWithPath>
{
    public GamePathTreeItemWithPath CreateItem(GamePath key) => new(key);
}
