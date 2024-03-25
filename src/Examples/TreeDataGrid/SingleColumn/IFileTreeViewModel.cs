using Avalonia.Controls;
using NexusMods.App.UI;
using NexusMods.App.UI.Controls.Trees.Files;

namespace Examples.TreeDataGrid.SingleColumn;

public interface IFileTreeViewModel : IViewModelInterface
{
    ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
}
