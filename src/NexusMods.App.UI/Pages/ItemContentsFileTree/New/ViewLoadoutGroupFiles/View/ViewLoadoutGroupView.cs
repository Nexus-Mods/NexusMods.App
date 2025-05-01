using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.ViewModel;
using ReactiveUI;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles.View;

public partial class ViewLoadoutGroupView : ReactiveUserControl<IViewLoadoutGroupFilesViewModel>
{
    public ViewLoadoutGroupView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            //this.OneWayBind(ViewModel, vm => vm.FileTreeViewModel, v => v.FilesTreeView.ViewModel)
            //    .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenEditorCommand, view => view.OpenEditorMenuItem)
                .DisposeWith(disposables);
            
            this.BindCommand(ViewModel, vm => vm.RemoveCommand, view => view.RemoveButton)
                .DisposeWith(disposables);
        });
    }
}

