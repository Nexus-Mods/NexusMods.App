using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.App.UI.Overlays.MetricsOptIn;

public partial class MetricsOptInView : ReactiveUserControl<IMetricsOptInViewModel>
{
    public MetricsOptInView()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.BindCommand(ViewModel, vm => vm.Allow, view => view.AllowButton)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Deny, view => view.DenyButton)
                .DisposeWith(d);
        });
    }
}

