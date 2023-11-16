using System.Reactive;
using System.Windows.Input;
using NexusMods.App.UI.Overlays;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IUnsupportedModPageViewModel : IViewModelInterface
{
    /// <summary>
    ///     Declares whether the mod should be 'advanced installed'.
    ///     If this is false, the advanced installer should not be executed, else this should be set to true.
    /// </summary>
    public bool WasAccepted { get; set; }

    /// <summary>
    /// Name of the mod to be installed.
    /// </summary>
    public string ModName { get; }

    /// <summary>
    ///     Command to accept the offer to install the mod.
    /// </summary>
    ReactiveCommand<Unit, Unit> AcceptCommand { get; }

    /// <summary>
    ///     Command to decline the offer to install the mod.
    /// </summary>
    ReactiveCommand<Unit, Unit> DeclineCommand { get; }
}
