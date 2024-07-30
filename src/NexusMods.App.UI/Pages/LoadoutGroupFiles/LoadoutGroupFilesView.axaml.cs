using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LoadoutGroupFiles;

public partial class LoadoutGroupFilesView : ReactiveUserControl<ILoadoutGroupFilesViewModel>
{
    public LoadoutGroupFilesView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.FileTreeViewModel, v => v.FilesTreeView.ViewModel)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.OpenEditorCommand, view => view.OpenEditorMenuItem)
                .DisposeWith(disposables);
        });
    }
}

