using System.Reactive;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public class GuidedInstallerWindowDesignViewModel : AViewModel<IGuidedInstallerWindowViewModel>, IGuidedInstallerWindowViewModel
{
    public string WindowName { get; set; } = "Test FOMOD Installer";

    public IGuidedInstallerStepViewModel? ActiveStepViewModel { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand => Initializers.EnabledReactiveCommand;
}
