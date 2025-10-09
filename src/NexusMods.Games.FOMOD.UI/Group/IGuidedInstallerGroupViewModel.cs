using System.Collections.ObjectModel;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.UI.Sdk;

namespace NexusMods.Games.FOMOD.UI;

public interface IGuidedInstallerGroupViewModel : IViewModelInterface
{
    public IObservable<bool> HasValidSelectionObservable { get; }

    public OptionGroup Group { get; }

    public ReadOnlyObservableCollection<IGuidedInstallerOptionViewModel> Options { get; }

    public IGuidedInstallerOptionViewModel? HighlightedOption { get; set;  }
}
