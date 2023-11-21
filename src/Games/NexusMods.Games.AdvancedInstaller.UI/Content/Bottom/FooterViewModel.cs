using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class FooterViewModel : AViewModel<IFooterViewModel>, IFooterViewModel
{
    [Reactive] public ReactiveCommand<Unit, Unit> CancelCommand { get; set; } = ReactiveCommand.Create(() => { });

    [Reactive] public ReactiveCommand<Unit, Unit> InstallCommand { get; set; } = ReactiveCommand.Create(() => { });
}
