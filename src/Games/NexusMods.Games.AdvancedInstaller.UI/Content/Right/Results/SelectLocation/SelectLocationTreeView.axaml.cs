using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public partial class SelectLocationTreeView : ReactiveUserControl<ISelectLocationTreeViewModel>
{
    public SelectLocationTreeView()
    {
        InitializeComponent();

        this.WhenActivated(disposable =>
            this.OneWayBind(ViewModel, vm => vm.Tree, view => view.SelectTreeDataGrid.Source )
        );
    }
}

