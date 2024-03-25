using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

public partial class ModFilesView : ReactiveUserControl<IModFilesViewModel>
{
    public ModFilesView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
            {
                this.OneWayBind(ViewModel, vm => vm.FileTreeViewModel, 
                        v => v.FilesTreeView.ViewModel)
                    .DisposeWith(disposables);
            }
        );
    }
}
