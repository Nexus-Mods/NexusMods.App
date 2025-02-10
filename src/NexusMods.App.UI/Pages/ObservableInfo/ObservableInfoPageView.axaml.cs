using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ObservableInfo;

public partial class ObservableInfoPageView : ReactiveUserControl<IObservableInfoPageViewModel>
{
    public ObservableInfoPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.TrackingStates, view => view.States.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.TrackingStates.Count, view => view.Count.Text, static i => $"Observable Count: {i}")
                .DisposeWith(disposables);
        });
    }
}
