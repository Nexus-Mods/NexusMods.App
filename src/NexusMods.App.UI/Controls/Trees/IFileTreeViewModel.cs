using System.Collections.ObjectModel;
using Avalonia.Controls;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls.Trees.Files;

namespace NexusMods.App.UI.Controls.Trees;

public interface IFileTreeViewModel : IViewModelInterface
{
    ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
    
    ReadOnlyObservableCollection<string> StatusBarStrings { get; }
    
    /// <summary>
    /// The invalid GamePath used to represent the parent of a root entry.
    /// Necessary for DynamicData TransformToTree, we need a GamePath that is guaranteed not to represent another node.
    /// </summary>
    public static readonly GamePath RootParentGamePath = new(LocationId.Unknown, "");
}
