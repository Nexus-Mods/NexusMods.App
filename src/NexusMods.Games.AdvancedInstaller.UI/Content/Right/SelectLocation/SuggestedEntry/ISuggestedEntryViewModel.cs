using System.Reactive;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

/// <summary>
/// Represents a known location where files are likely to be installed to.
/// Should show all the game Locations (LocationId), optionally nested ones (e.g. Data folder in Skyrim).
/// </summary>
public interface ISuggestedEntryViewModel : IViewModelInterface
{
    /// <summary>
    /// The bold text displayed for this entry.
    /// Should be either the LocationId text or the folder name.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The subtitle text displayed for this entry.
    /// Should be the full absolute path of the location.
    /// </summary>
    public string Subtitle { get; }

    /// <summary>
    /// Unique identifier for this entry. Required to use DynamicData changesets.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// The GamePath relative to a top level location.
    /// </summary>
    public GamePath RelativeToTopLevelLocation { get; }

    /// <summary>
    /// This is invoked when the user clicks the 'Select' button.
    /// Will cause the mapping of the selected mod content entries under the chosen folder location.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateMappingCommand { get; }
}
