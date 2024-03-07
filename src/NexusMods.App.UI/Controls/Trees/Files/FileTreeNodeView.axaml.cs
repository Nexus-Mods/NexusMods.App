using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees.Files;

public partial class FileTreeNodeView : ReactiveUserControl<IFileTreeNodeViewModel>
{
    private FileTreeNodeIconType _lastType = FileTreeNodeIconType.File;
    
    public FileTreeNodeView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
            {
                ViewModel.WhenAnyValue(vm => vm.Icon)
                    .Subscribe(iconType =>
                    {
                        EntryIcon.Classes.Remove(_lastType.GetIconClass());
                        EntryIcon.Classes.Add(iconType.GetIconClass());
                        _lastType = iconType;
                    })
                    .DisposeWith(d);
                FileNameTextBlock.Text = ViewModel!.Name;
            }
        );
    }
}

