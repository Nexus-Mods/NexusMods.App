using System.Reactive;
using Avalonia.Media;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerStepViewModel : IViewModelInterface
{
    public string? ModName { get; set; }
    public GuidedInstallationStep? InstallationStep { get; set; }

    public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }

    public string? HighlightedOptionDescription { get; set; }

    public IObservable<IImage> HighlightedOptionImageObservable { get; }

    public IGuidedInstallerGroupViewModel[] Groups { get; set; }

    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    public Percent Progress { get; set; }

    public ReactiveCommand<Unit, Unit> NextStepCommand { get; set; }

    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; set; }

    public ReactiveCommand<Unit, Unit> CancelInstallerCommand { get; set; }
}
