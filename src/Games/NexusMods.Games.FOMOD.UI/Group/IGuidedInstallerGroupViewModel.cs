using System.Collections.ObjectModel;
using NexusMods.App.UI;
using NexusMods.Common.GuidedInstaller;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerGroupViewModel : IViewModelInterface
{
    public IObservable<bool> HasValidSelectionObservable { get; }

    public OptionGroup Group { get; }

    public ReadOnlyObservableCollection<IGuidedInstallerOptionViewModel> Options { get; }

    public IGuidedInstallerOptionViewModel? HighlightedOption { get; set;  }
}
