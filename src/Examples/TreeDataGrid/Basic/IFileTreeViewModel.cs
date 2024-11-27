using Avalonia.Controls;
using Examples.TreeDataGrid.Basic.ViewModel;
using NexusMods.Abstractions.UI;

namespace Examples.TreeDataGrid.Basic;

public interface IFileTreeViewModel : IViewModelInterface
{
    ITreeDataGridSource<IFileViewModel>? TreeSource { get; }
}
