using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees.Files;

public partial class FileTreeNodeView : ReactiveUserControl<IFileTreeNodeViewModel>
{
    public FileTreeNodeView()
    {
        InitializeComponent();
        
        // We don't need to subscribe in 'WhenActivated', because the state does not mutate.
        this.WhenActivated(_ =>
            {
                FileEntryIcon.IsVisible = ViewModel!.IsFile;
                FolderEntryIcon.IsVisible = !ViewModel!.IsFile;
                FileNameTextBlock.Text = ViewModel!.Name;
            }
        );
    }
}

