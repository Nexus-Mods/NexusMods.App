using Avalonia.Controls;
using NexusMods.App.UI.Controls.Trees.Files;

namespace NexusMods.App.UI.Controls.Trees;

public class FileTreeDesignViewModel : AViewModel<IFileTreeViewModel>, IFileTreeViewModel
{
    public ITreeDataGridSource<IFileTreeNodeViewModel> TreeSource { get; } = null!;
}
