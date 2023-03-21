using System.Reactive;
using NexusMods.DataModel.Games;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LaunchButtonDesignViewModel : AViewModel<ILaunchButtonViewModel>, ILaunchButtonViewModel
{
    public IGame Game { get; set; } = GameInstallation.Empty.Game;
    public ReactiveCommand<Unit, Unit> Command { get; } = Initializers.ReactiveCommandUnitUnit;
    public string Label { get; } = "Launch";
}
