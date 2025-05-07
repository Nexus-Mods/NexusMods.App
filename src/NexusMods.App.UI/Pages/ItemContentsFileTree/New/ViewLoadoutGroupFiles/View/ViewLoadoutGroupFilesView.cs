using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.View;

[UsedImplicitly]
public partial class ViewLoadoutGroupFilesView : ReactiveUserControl<IViewLoadoutGroupFilesViewModel>
{
    public ViewLoadoutGroupFilesView()
    {
        InitializeComponent();
        TreeDataGridViewHelper.SetupTreeDataGridAdapter<ViewLoadoutGroupFilesView, IViewLoadoutGroupFilesViewModel, CompositeItemModel<EntityId>, EntityId>(this, TreeDataGrid, vm => vm.FileTreeAdapter!);
        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.OpenEditorCommand, view => view.OpenEditorMenuItem)
                .DisposeWith(disposables);
            
            this.BindCommand(ViewModel, vm => vm.RemoveCommand, view => view.RemoveButton)
                .DisposeWith(disposables);
        });
    }
}

