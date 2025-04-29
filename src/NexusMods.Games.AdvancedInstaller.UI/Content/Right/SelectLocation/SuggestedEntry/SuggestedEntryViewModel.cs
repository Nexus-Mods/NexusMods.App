using System.Reactive;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
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

    /// <summary>
    /// Constructs a new SuggestedEntryViewModel representing a known install location.
    /// </summary>
    /// <param name="id">A unique GUID</param>
    /// <param name="absolutePath">The absolute path of the location</param>
    /// <param name="associatedLocation">The LocationId associated to this location.</param>
    /// <param name="relativeToTopLevelLocation">The GamePath relative to a top level location</param>
    /// <param name="title">The title to show, the associatedLocation name is used if empty</param>
    /// <param name="subtitle">The subtitle to show, the absolutePath is used if empty</param>
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

        Title = string.IsNullOrEmpty(title) ? associatedLocation.ToString() : title;
        Subtitle = string.IsNullOrEmpty(subtitle) ? AbsolutePath.ToString() : subtitle;

        CreateMappingCommand = ReactiveCommand.Create(() => { });
    }
}
