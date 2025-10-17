using System.Reactive;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerWindowViewModel : IViewModelInterface
{
    public string WindowName { get; set; }

    public IGuidedInstallerStepViewModel? ActiveStepViewModel { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
}
