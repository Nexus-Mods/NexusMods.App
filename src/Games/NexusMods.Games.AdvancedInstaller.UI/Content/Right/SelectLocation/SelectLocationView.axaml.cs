using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public partial class SelectLocationView : ReactiveUserControl<ISelectLocationViewModel>
{
    public SelectLocationView()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.SuggestedAreaSubtitle,
                    view => view.SuggestedSubHeaderText.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.SuggestedEntries,
                    view => view.SuggestedLocationItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm =>
                    vm.Tree, view => view.SelectTreeDataGrid.Source)
                .DisposeWith(disposables);
        });
    }
}
