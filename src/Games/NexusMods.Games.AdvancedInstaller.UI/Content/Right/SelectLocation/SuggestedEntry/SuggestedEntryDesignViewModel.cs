using System.Reactive;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

internal class SuggestedEntryDesignViewModel : AViewModel<ISuggestedEntryViewModel>,
    ISuggestedEntryViewModel
{

    // Needed for design time
    public SuggestedEntryDesignViewModel()
    {
        CorrespondingNode = new SelectableTreeEntryStandardDirectoryDesignViewModel();
        Title = "Title";
        Subtitle = "Subtitle";
        SelectCommand = ReactiveCommand.Create(() => { });

    }

    public string Title { get; }
    public string Subtitle { get; }
    public ISelectableTreeEntryViewModel CorrespondingNode { get; }
    public ReactiveCommand<Unit, Unit> SelectCommand { get; }
}
