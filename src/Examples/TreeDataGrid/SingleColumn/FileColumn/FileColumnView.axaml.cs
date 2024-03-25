using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Examples.TreeDataGrid.SingleColumn.FileColumn;

public partial class FileColumnView : ReactiveUserControl<IFileTreeNodeViewModel>
{
    private FileTreeNodeIconType _lastType = FileTreeNodeIconType.File;
    
    public FileColumnView()
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
                
                // Subscribe here if you need mutating.
                FileNameTextBlock.Text = ViewModel!.Name;
            }
        );
    }
}

