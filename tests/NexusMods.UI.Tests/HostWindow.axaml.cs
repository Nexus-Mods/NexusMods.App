using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.UI.Tests;

public partial class HostWindow : ReactiveWindow<HostWindowViewModel>
{
    public HostWindow()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.ViewModel!.Content)
                .BindTo(this, x => x.Host.ViewModel)
                .DisposeWith(d);
        });
#if DEBUG
        this.AttachDevTools();
#endif
    }
}

