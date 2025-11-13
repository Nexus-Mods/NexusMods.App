using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.App.UI.Helpers.TreeDataGrid.New.FolderGenerator;

/// <summary>
/// Represents an item (file) inserted into the tree with a given path.
/// </summary>
public interface ITreeItemWithPath
{
    /// <summary>
    /// The path of the item, represented as the <see cref="LocationId"/>
    /// and its corresponding <see cref="RelativePath"/>
    /// </summary>
    GamePath GetPath();
}

/// <summary>
/// Factory for the <see cref="GamePathTreeItemWithPath"/> type.
/// </summary>
public interface ITreeItemWithPathFactory<in TKey, out TTreeItemWithPath> where TTreeItemWithPath : ITreeItemWithPath
{
    /// <summary>
    /// The path of the item, represented as the <see cref="LocationId"/>
    /// and its corresponding <see cref="RelativePath"/>
    /// </summary>
    /// <paramref name="key">
    ///     The key from which the item is created.
    ///     For example a key into a dictionary, or an <see cref="EntityId"/> if we're representing a database object.
    /// </paramref>
    TTreeItemWithPath CreateItem(TKey key);
}
