using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public partial class AdvancedInstallerWindowView : ReactiveWindow<IAdvancedInstallerWindowViewModel>
{
    public AdvancedInstallerWindowView()
    {
        InitializeComponent();


        this.WhenActivated(disposables =>
        {
            ViewModel!.UnsupportedModVM.DeclineCommand
                .Subscribe(_ => this.Close())
                .DisposeWith(disposables);

            ViewModel!.AdvancedInstallerVM.FooterViewModel.CancelCommand
                .Subscribe(_ => Close())
                .DisposeWith(disposables);

            ViewModel!.AdvancedInstallerVM.FooterViewModel.InstallCommand
                .Subscribe(_ => Close())
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.CurrentPageVM,
                    v => v.CurrentPage.ViewModel)
                .DisposeWith(disposables);
        });
    }
}
