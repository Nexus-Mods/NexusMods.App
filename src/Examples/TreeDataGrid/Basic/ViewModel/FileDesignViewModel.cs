using System.Collections.ObjectModel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using ReactiveUI.Fody.Helpers;

namespace Examples.TreeDataGrid.Basic.ViewModel;

public class FileDesignViewModel(GamePath fullPath) : AViewModel<IFileViewModel>, IFileViewModel
{
    // Name of this node.
    public string Name => Key.Name;

    // IDynamicDataTreeItem
    public ReadOnlyObservableCollection<IFileViewModel>? Children { get; set; }
    public IFileViewModel? Parent { get; set; }
    public GamePath Key { get; set; } = fullPath;

    // IExpandableViewModel
    [Reactive] public bool IsExpanded { get; set; }
}
