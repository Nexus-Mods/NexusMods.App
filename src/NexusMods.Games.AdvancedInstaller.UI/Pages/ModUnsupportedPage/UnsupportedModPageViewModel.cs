using System.Reactive;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class UnsupportedModPageViewModel : AViewModel<IUnsupportedModPageViewModel>,
    IUnsupportedModPageViewModel
{
    public UnsupportedModPageViewModel(string modName)
    {
        ModName = modName;
        AcceptCommand = ReactiveCommand.Create(() => { });
        DeclineCommand = ReactiveCommand.Create(() => { });
    }

    /// <inheritdoc />
    public string ModName { get; }

    /// <inheritdoc />
    public ReactiveCommand<Unit, Unit> AcceptCommand { get; }

    /// <inheritdoc />
    public ReactiveCommand<Unit, Unit> DeclineCommand { get; }
}
