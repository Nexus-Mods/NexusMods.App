using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Examples.TreeDataGrid.SingleColumn.FileColumn;

public partial class FileColumnView : ReactiveUserControl<IFileColumnViewModel>
{
    public FileColumnView()
    {
        InitializeComponent();

        // Subscribe here if you need mutating.
        this.WhenActivated(_ =>
            {
                EntryIcon.Classes.Add(ViewModel!.IsFile ? "File" : "FolderOutline");
                FileNameTextBlock.Text = ViewModel!.Name;
            }
        );
    }
}

