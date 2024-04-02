using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Examples.TreeDataGrid.SingleColumn.FileColumn;
using JetBrains.Annotations;
using ReactiveUI;

namespace Examples.TreeDataGrid.SingleColumn;

[UsedImplicitly] // Designer
public partial class FileTreeView : ReactiveUserControl<IFileTreeViewModel>
{
    public FileTreeView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
            {
                this.OneWayBind<IFileTreeViewModel, FileTreeView, ITreeDataGridSource<IFileColumnViewModel>, ITreeDataGridSource>
                    (ViewModel, vm => vm.TreeSource, v => v.ModFilesTreeDataGrid.Source!)
                    .DisposeWith(disposables);
                
                // This is a workaround for TreeDataGrid collapsing Star sized columns.
                // This forces a refresh of the width, fixing the issue.
                ModFilesTreeDataGrid.Width = double.NaN;
            }
        );
    }
}

