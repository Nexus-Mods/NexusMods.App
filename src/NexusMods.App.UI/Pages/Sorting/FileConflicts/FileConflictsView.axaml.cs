using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.UI.Sdk;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public partial class FileConflictsView : R3UserControl<IFileConflictsViewModel>
{
    public FileConflictsView()
    {
        InitializeComponent();

        TreeDataGridViewHelper.SetupTreeDataGridAdapter<FileConflictsView, IFileConflictsViewModel, CompositeItemModel<EntityId>, EntityId>(
            this, TreeDataGrid, vm => vm.TreeDataGridAdapter);

        this.WhenActivated(disposables =>
        {
            this.OneWayR3Bind(view => view.BindableViewModel, vm => vm.TreeDataGridAdapter.Source, (view, source) => view.TreeDataGrid.Source = source)
                .AddTo(disposables);
        });
    }
}

