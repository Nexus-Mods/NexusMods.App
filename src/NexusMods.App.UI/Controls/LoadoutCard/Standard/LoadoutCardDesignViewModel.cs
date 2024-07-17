using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.App.UI.Controls.LoadoutBadge;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.LoadoutCard;

public class LoadoutCardDesignViewModel : AViewModel<ILoadoutCardViewModel>, ILoadoutCardViewModel
{
    public LoadoutCardDesignViewModel()
    {
        this.WhenActivated(d =>
            {
                // Cycle thorough all the states for preview purposes
                Observable.Interval(TimeSpan.FromSeconds(2.5))
                    .Subscribe(_ =>
                    {
                        IsDeleting = !IsDeleting;
                    })
                    .DisposeWith(d);
            }
        );
    }
    
    public ILoadoutBadgeViewModel LoadoutBadgeViewModel { get; } = new LoadoutBadgeDesignViewModel();
    public string LoadoutName { get; } = "Loadout B";
    public IImage LoadoutImage { get; } = new Bitmap(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/cyberpunk_game.png")));

    public bool IsLoadoutApplied { get; } = true;
    public string HumanizedLoadoutLastApplyTime { get; } = "Last applied 2 months ago";
    public string HumanizedLoadoutCreationTime { get; } = "Created 10 months ago";
    public string LoadoutModCount { get; } = "Mods 276";
    [Reactive] public bool IsDeleting { get; private set; } = true;
    public bool IsSkeleton { get; } = false;
    public ReactiveCommand<Unit, Unit> VisitLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> CloneLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> DeleteLoadoutCommand { get; } = ReactiveCommand.Create(() => { });
}
