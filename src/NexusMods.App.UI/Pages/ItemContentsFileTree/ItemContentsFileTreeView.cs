using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ItemContentsFileTree;

public partial class ItemContentsFileTreeView : ReactiveUserControl<IItemContentsFileTreeViewModel>
{
    public ItemContentsFileTreeView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.FileTreeViewModel, v => v.FilesTreeView.ViewModel)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenEditorCommand, view => view.OpenEditorMenuItem)
                .DisposeWith(disposables);
            
            this.BindCommand(ViewModel, vm => vm.RemoveCommand, view => view.RemoveButton)
                .DisposeWith(disposables);
        });
    }
}

