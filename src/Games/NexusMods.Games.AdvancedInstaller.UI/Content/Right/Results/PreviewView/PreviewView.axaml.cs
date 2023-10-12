using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public partial class PreviewView : ReactiveUserControl<IPreviewViewModel>
{
    public PreviewView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind<
                        IPreviewViewModel, PreviewView, HierarchicalTreeDataGridSource<ITreeEntryViewModel>,
                        ITreeDataGridSource>
                    (ViewModel, vm => vm.Tree, view => view.LocationPreviewTreeDataGrid.Source!)
                .DisposeWith(disposables);
        });
    }
}
