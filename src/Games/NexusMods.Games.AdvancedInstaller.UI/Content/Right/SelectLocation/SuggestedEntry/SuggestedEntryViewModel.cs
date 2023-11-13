using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SuggestedEntryViewModel : AViewModel<ISuggestedEntryViewModel>, ISuggestedEntryViewModel
{
    public string Title { get; }
    public string Subtitle { get; }

    public Guid Id { get; }
    public AbsolutePath AbsolutePath { get; }
    public LocationId AssociatedLocation { get; }
    public GamePath RelativeToTopLevelLocation { get; }
    public ReactiveCommand<Unit, Unit> CreateMappingCommand { get; }

    public SuggestedEntryViewModel(
        Guid id,
        AbsolutePath absolutePath,
        LocationId associatedLocation,
        GamePath relativeToTopLevelLocation,
        string title = "",
        string subtitle = "")
    {
        Id = id;
        AbsolutePath = absolutePath;
        AssociatedLocation = associatedLocation;
        RelativeToTopLevelLocation = relativeToTopLevelLocation;

        Title = title == string.Empty ? absolutePath.FileName : title;
        Subtitle = subtitle == string.Empty ? associatedLocation.Value : subtitle;

        CreateMappingCommand = ReactiveCommand.Create(() => { });
    }
}
