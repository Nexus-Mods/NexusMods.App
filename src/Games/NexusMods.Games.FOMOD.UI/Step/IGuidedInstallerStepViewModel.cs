using Avalonia.Media;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerStepViewModel : IViewModelInterface
{
    public string? ModName { get; set; }
    public GuidedInstallationStep? InstallationStep { get; set; }

    public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }

    public IObservable<IImage> HighlightedOptionImageObservable { get; }

    public IGuidedInstallerGroupViewModel[] Groups { get; set; }

    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }

    public Percent Progress { get; set; }

    public bool ShowInstallationCompleteScreen { get; set; }

    public IFooterStepperViewModel FooterStepperViewModel { get; }
}
