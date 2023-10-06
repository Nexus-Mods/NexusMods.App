using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Left;

internal partial class ModContentView : ReactiveUserControl<IModContentViewModel>
{
    public ModContentView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind<IModContentViewModel, ModContentView, HierarchicalTreeDataGridSource<IModContentFileNode>,
                    ITreeDataGridSource>(ViewModel, vm => vm.Tree, view => view.ModContentTreeDataGrid.Source!)
                .DisposeWith(disposables);
        });
    }
}
