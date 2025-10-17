using System.Reactive;
using Avalonia.Media;
using NexusMods.App.UI.Controls.LoadoutBadge;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public class SkeletonLoadoutCardViewModel : AViewModel<ILoadoutCardViewModel>, ILoadoutCardViewModel 
{
    public ILoadoutBadgeViewModel LoadoutBadgeViewModel { get; } = new LoadoutBadgeDesignViewModel();
    public required string LoadoutName { get; init; }
    public required IImage LoadoutImage { get; init; } 
    public bool IsLoadoutApplied { get; } = false;
    public string HumanizedLoadoutLastApplyTime { get; } = "";
    public string HumanizedLoadoutCreationTime { get; } = "";
    public string LoadoutModCount { get; } = "";
    public bool IsDeleting { get; } = false;
    public bool IsSkeleton { get; } = true;
    public bool IsLastLoadout { get; } = false;
    public ReactiveCommand<Unit, Unit> VisitLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> CloneLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> DeleteLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
}
