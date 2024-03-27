using Avalonia.Controls;
using Examples.TreeDataGrid.SingleColumn.FileColumn;
using NexusMods.App.UI;

namespace Examples.TreeDataGrid.SingleColumn;

public interface IFileTreeViewModel : IViewModelInterface
{
    ITreeDataGridSource<IFileColumnViewModel>? TreeSource { get; }
}
