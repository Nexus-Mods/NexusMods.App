using System.Collections.ObjectModel;
using Avalonia.Media;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.RateLimiting;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerStepViewModel : IViewModelInterface
{
    public string ModName { get; set; }

    public bool ShowInstallationCompleteScreen { get; }
    public GuidedInstallationStep? InstallationStep { get; set; }

    public ReadOnlyObservableCollection<IGuidedInstallerGroupViewModel> Groups { get; }
    public IGuidedInstallerOptionViewModel? HighlightedOptionViewModel { get; set; }
    public IImage? HighlightedOptionImage { get; }

    public Percent Progress { set; }
    public IFooterStepperViewModel FooterStepperViewModel { get; }

    public TaskCompletionSource<UserChoice>? TaskCompletionSource { get; set; }
}
