using NexusMods.App.UI;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerWindowViewModel : IViewModelInterface
{
    public string WindowName { get; set; }

    public IGuidedInstallerStepViewModel? ActiveStepViewModel { get; set; }
}
