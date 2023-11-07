using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

internal class FooterViewModel : AViewModel<IFooterViewModel>, IFooterViewModel
{
    [Reactive]
    public ReactiveCommand<Unit, Unit> CancelCommand { get; set; } = Initializers.DisabledReactiveCommand;

    [Reactive]
    public ReactiveCommand<Unit, Unit> InstallCommand { get; set; } = Initializers.DisabledReactiveCommand;
}
