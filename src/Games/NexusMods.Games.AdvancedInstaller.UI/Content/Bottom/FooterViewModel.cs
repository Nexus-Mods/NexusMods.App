using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class FooterViewModel : AViewModel<IFooterViewModel>, IFooterViewModel
{
    public FooterViewModel()
    {
        CancelCommand = ReactiveCommand.Create(() => { });
        InstallCommand = ReactiveCommand.Create(() => { });
    }

    [Reactive]
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    [Reactive]
    public ReactiveCommand<Unit, Unit> InstallCommand { get; set; }
}
