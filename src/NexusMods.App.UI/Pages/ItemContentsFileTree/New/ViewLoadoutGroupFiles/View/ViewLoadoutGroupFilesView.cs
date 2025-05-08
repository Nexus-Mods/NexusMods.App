using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel;
using ReactiveUI;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.View;

[UsedImplicitly]
public partial class ViewLoadoutGroupFilesView : ReactiveUserControl<IViewLoadoutGroupFilesViewModel>
{
    public ViewLoadoutGroupFilesView()
    {
        InitializeComponent();
        TreeDataGridViewHelper.SetupTreeDataGridAdapter<ViewLoadoutGroupFilesView, IViewLoadoutGroupFilesViewModel, CompositeItemModel<GamePath>, GamePath>(this, TreeDataGrid, vm => vm.FileTreeAdapter!);
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.FileTreeAdapter!.Source.Value, view => view.TreeDataGrid.Source)
                .DisposeWith(disposables);
            
            this.BindCommand(ViewModel, vm => vm.OpenEditorCommand, view => view.OpenEditorMenuItem)
                .DisposeWith(disposables);
            
            this.BindCommand(ViewModel, vm => vm.RemoveCommand, view => view.RemoveButton)
                .DisposeWith(disposables);
        });
    }
}

