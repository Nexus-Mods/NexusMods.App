using System.Reactive;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.FOMOD.UI;

[UsedImplicitly]
public class GuidedInstallerWindowViewModel : AViewModel<IGuidedInstallerWindowViewModel>, IGuidedInstallerWindowViewModel
{
    [Reactive] public string WindowName { get; set; } = string.Empty;

    [Reactive]
    public IGuidedInstallerStepViewModel? ActiveStepViewModel { get; set; }

    public ReactiveCommand<Unit, Unit> CloseCommand => ReactiveCommand.Create(() => { });
}
