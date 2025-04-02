using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
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
