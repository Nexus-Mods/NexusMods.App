using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees.Files;

public partial class FileTreeNodeView : ReactiveUserControl<IFileTreeNodeViewModel>
{
    public FileTreeNodeView()
    {
        InitializeComponent();
        
        // We don't need to subscribe in 'WhenActivated', because the state does not mutate.
        this.WhenActivated(d =>
            {
                ViewModel.WhenAnyValue(vm => vm.Icon)
                    .Subscribe(iconType =>
                    {
                        FileEntryIcon.IsVisible = iconType == FileTreeNodeIconType.File;
                        FolderEntryIcon.IsVisible = iconType == FileTreeNodeIconType.ClosedFolder;
                        FolderOpenEntryIcon.IsVisible = iconType == FileTreeNodeIconType.OpenFolder;
                    })
                    .DisposeWith(d);
                FileNameTextBlock.Text = ViewModel!.Name;
            }
        );
    }
}

