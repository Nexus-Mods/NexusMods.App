using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class FooterViewModel : AViewModel<IFooterViewModel>, IFooterViewModel
{
    [Reactive] public bool CanInstall { get; set; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> InstallCommand { get; }

    public FooterViewModel()
    {
        CancelCommand = ReactiveCommand.Create(() => { });
        InstallCommand = ReactiveCommand.Create(() => { },
            this.WhenAnyValue(x => x.CanInstall));
    }
}
