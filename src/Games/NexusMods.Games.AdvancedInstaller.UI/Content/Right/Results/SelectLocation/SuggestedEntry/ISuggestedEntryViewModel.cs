using System.Reactive;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public interface ISuggestedEntryViewModel : IViewModel
{
    public string Title { get; }

    public string Subtitle { get; }

    public ITreeEntryViewModel CorrespondingNode { get; }

    public ReactiveCommand<Unit, Unit> SelectCommand { get; }
}
