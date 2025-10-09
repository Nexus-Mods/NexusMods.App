using System.Reactive;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IUnsupportedModPageViewModel : IViewModelInterface
{
    /// <summary>
    /// Name of the mod to be installed.
    /// </summary>
    public string ModName { get; }

    /// <summary>
    /// Command to accept the offer to install the mod.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AcceptCommand { get; }

    /// <summary>
    /// Command to decline the offer to install the mod.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeclineCommand { get; }
}
