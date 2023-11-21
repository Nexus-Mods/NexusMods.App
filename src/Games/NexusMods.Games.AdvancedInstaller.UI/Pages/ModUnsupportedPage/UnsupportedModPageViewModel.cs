using System.Reactive;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class UnsupportedModPageViewModel : AViewModel<IUnsupportedModPageViewModel>,
    IUnsupportedModPageViewModel
{
    public UnsupportedModPageViewModel(string modName)
    {
        WasAccepted = false;
        ModName = modName;
        AcceptCommand = ReactiveCommand.Create(() => { WasAccepted = true; });
        DeclineCommand = ReactiveCommand.Create(() => { });
    }

    /// <inheritdoc />
    public string ModName { get; }

    /// <inheritdoc />
    public ReactiveCommand<Unit, Unit> AcceptCommand { get; }

    /// <inheritdoc />
    public ReactiveCommand<Unit, Unit> DeclineCommand { get; }

    /// <inheritdoc />
    [Reactive]
    public bool WasAccepted { get; set; }
}
