using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
            this.WhenAnyValue(v => v.ViewModel!.Allow)
                .BindTo(this, view => view.AllowButton.Command)
                .DisposeWith(d);

            this.WhenAnyValue(v => v.ViewModel!.Deny)
                .BindTo(this, view => view.DenyButton.Command)
                .DisposeWith(d);
        });
    }
}

