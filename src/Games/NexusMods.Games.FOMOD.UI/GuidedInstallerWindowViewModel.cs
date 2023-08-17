using JetBrains.Annotations;
using NexusMods.App.UI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public class GuidedInstallerWindowViewModel : AViewModel<IGuidedInstallerWindowViewModel>, IGuidedInstallerWindowViewModel
{
    [Reactive] public string WindowName { get; set; } = string.Empty;

    [Reactive]
    public IGuidedInstallerStepViewModel? ActiveStepViewModel { get; set; }
}
