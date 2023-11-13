using System.Collections;
using System.Collections.ObjectModel;
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
            this.OneWayBind(ViewModel, vm => vm.SuggestedEntries,
                    view => view.SuggestedLocationItemsControl.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.TreeContainers,
                    view => view.AllFoldersItemsControl.ItemsSource)
                .DisposeWith(disposables);
        });
    }
}
