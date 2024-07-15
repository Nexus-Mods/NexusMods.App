using System.Reactive;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.App.UI.Controls.LoadoutBadge;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public class SkeletonLoadoutCardViewModel : AViewModel<ILoadoutCardViewModel>, ILoadoutCardViewModel 
{
    public ILoadoutBadgeViewModel LoadoutBadgeViewModel { get; } = new LoadoutBadgeDesignViewModel();
    public string LoadoutName { get; } = "Loadout B";
    public IImage LoadoutImage { get; } = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));
    public bool IsLoadoutApplied { get; } = false;
    public string HumanizedLoadoutLastApplyTime { get; } = "";
    public string HumanizedLoadoutCreationTime { get; } = "";
    public string LoadoutModCount { get; } = "";
    public bool IsDeleting { get; } = false;
    public bool IsSkeleton { get; } = true;
    public ReactiveCommand<Unit, Unit> VisitLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> CloneLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> DeleteLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
}
