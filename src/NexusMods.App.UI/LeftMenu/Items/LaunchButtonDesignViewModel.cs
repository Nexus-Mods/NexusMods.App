using System.Reactive;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.RateLimiting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonDesignViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    [Reactive]
    public IGame Game { get; set; } = GameInstallation.Empty.Game;
    
    [Reactive]
    public ReactiveCommand<Unit, Unit> Command { get; set; } = Initializers.ReactiveCommandUnitUnit;
    
    [Reactive]
    public string Label { get; set; } = "Launch";

    [Reactive]
    public Percent? Progress { get; set; }
}
