using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

[UsedImplicitly]
public partial class ModFilesView : ReactiveUserControl<IModFilesViewModel>
{
    public ModFilesView()
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
