using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGroupFilesPage;

[UsedImplicitly]
public partial class LoadoutGroupFilesView : ReactiveUserControl<ILoadoutGroupFilesViewModel>
{
    public LoadoutGroupFilesView()
    {
        InitializeComponent();
        TreeDataGridViewHelper.SetupTreeDataGridAdapter<LoadoutGroupFilesView, ILoadoutGroupFilesViewModel, CompositeItemModel<GamePath>, GamePath>(this, TreeDataGrid, vm => vm.FileTreeAdapter!);
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.FileTreeAdapter!.Source.Value, view => view.TreeDataGrid.Source)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.OpenEditorCommand, view => view.OpenEditorMenuItem)
                .AddTo(disposables);
            
            this.BindCommand(ViewModel, vm => vm.RemoveCommand, view => view.RemoveButton)
                .AddTo(disposables);
        });
    }
}

