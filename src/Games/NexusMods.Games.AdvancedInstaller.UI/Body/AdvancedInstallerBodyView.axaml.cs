using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public partial class AdvancedInstallerBodyView : ReactiveUserControl<IAdvancedInstallerBodyViewModel>
{
    public AdvancedInstallerBodyView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.ModContentViewModel, view => view.ModContentSectionViewHost.ViewModel)
                .DisposeWith(disposables);
        });
    }
}
