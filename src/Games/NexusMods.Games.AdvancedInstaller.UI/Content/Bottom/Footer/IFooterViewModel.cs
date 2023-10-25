using System.Reactive;
using System.Windows.Input;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

public interface IFooterViewModel : IViewModel
{
    ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    ReactiveCommand<Unit, Unit> InstallCommand { get; set; }
}
