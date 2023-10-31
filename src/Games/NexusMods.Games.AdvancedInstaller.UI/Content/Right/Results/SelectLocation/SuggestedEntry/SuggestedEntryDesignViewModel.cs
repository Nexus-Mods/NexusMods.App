using System.Reactive;
using NexusMods.DataModel.Games;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SuggestedEntryDesignViewModel : AViewModel<ISuggestedEntryViewModel>,
    ISuggestedEntryViewModel
{

    // Needed for design time
    public SuggestedEntryDesignViewModel()
    {
        CorrespondingNode = new TreeEntryStandardDirectoryDesignViewModel();
        Title = "Title";
        Subtitle = "Subtitle";
        SelectCommand = ReactiveCommand.Create(() => { });

    }

    public string Title { get; }
    public string Subtitle { get; }
    public ITreeEntryViewModel CorrespondingNode { get; }
    public ReactiveCommand<Unit, Unit> SelectCommand { get; }
}
