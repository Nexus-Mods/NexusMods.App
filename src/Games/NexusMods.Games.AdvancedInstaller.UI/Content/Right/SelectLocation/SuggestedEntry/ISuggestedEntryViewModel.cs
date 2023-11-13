using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public interface ISuggestedEntryViewModel : IViewModelInterface
{
    public string Title { get; }
    public string Subtitle { get; }

    public Guid Id { get; }

    public AbsolutePath AbsolutePath { get; }
    public LocationId AssociatedLocation { get; }

    public GamePath RelativeToTopLevelLocation { get; }

    public ReactiveCommand<Unit, Unit> CreateMappingCommand { get; }
}
