using System.Reactive;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public interface ISuggestedEntryViewModel : IViewModelInterface
{
    public string Title { get; }

    public string Subtitle { get; }

    public ISelectableTreeEntryViewModel CorrespondingNode { get; }

    public ReactiveCommand<Unit, Unit> SelectCommand { get; }
}
