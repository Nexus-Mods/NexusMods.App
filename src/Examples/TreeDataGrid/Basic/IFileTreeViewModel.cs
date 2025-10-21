using Avalonia.Controls;
using Examples.TreeDataGrid.Basic.ViewModel;
using NexusMods.UI.Sdk;

namespace Examples.TreeDataGrid.Basic;

public interface IFileTreeViewModel : IViewModelInterface
{
    ITreeDataGridSource<IFileViewModel>? TreeSource { get; }
}
