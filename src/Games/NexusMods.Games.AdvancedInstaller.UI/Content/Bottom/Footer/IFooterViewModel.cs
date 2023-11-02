using System.Reactive;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;

public interface IFooterViewModel : IViewModel
{
    ReactiveCommand<Unit, Unit> CancelCommand { get; set; }

    ReactiveCommand<Unit, Unit> InstallCommand { get; set; }
}
