using Avalonia.ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees;

public partial class FileTreeView : ReactiveUserControl<IFileTreeViewModel>
{
    public FileTreeView()
    {
        InitializeComponent();
    }
}

