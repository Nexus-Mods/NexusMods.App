using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

[ExcludeFromCodeCoverage]
public partial class AdvancedInstallerPageView : ReactiveUserControl<IAdvancedInstallerPageViewModel>
{
    public AdvancedInstallerPageView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.BodyViewModel,
                    v => v.TopContentViewHost.ViewModel)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.FooterViewModel,
                    v => v.BottomContentViewHost.ViewModel)
                .DisposeWith(disposables);
        });
    }
}
