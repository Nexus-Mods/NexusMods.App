using Avalonia.Controls;
using NexusMods.App.UI.Controls.Trees.Files;

namespace NexusMods.App.UI.Controls.Trees;

public interface IFileTreeViewModel : IViewModelInterface
{
    ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; }
}
