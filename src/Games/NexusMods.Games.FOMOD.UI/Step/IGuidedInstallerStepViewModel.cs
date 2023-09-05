using System.Reactive;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerStepViewModel : IViewModelInterface
{
    public GuidedInstallationStep? InstallationStep { get; set; }

    public IGuidedInstallerGroupViewModel[] Groups { get; set; }

    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; set; }

    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; set; }

    public ReactiveCommand<Unit, Unit> CancelInstallerCommand { get; set; }
}
