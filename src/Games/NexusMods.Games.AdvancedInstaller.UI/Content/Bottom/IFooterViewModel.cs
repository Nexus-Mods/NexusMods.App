using System.Reactive;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IFooterViewModel : IViewModelInterface
{
    /// <summary>
    /// Determines whether the Install button is enabled or not.
    /// </summary>
    bool CanInstall { get; set; }

    ReactiveCommand<Unit, Unit> CancelCommand { get;  }

    ReactiveCommand<Unit, Unit> InstallCommand { get; }
}
