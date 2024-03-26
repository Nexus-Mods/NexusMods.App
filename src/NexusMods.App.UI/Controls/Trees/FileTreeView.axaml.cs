using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.Trees;

public partial class FileTreeView : ReactiveUserControl<IFileTreeViewModel>
{
    public FileTreeView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => 
                        vm.TreeSource, v => v.ModFilesTreeDataGrid.Source)
                    .DisposeWith(disposables);
                
                this.OneWayBind(ViewModel, vm => 
                        vm.StatusBarStrings, v => v.StatusBarItemsControl.ItemsSource)
                    .DisposeWith(disposables);
                
                // This is a workaround for TreeDataGrid collapsing Star sized columns.
                // This forces a refresh of the width, fixing the issue.
                ModFilesTreeDataGrid.Width = double.NaN;
            }
        );
    }
}

