using NexusMods.Abstractions.Games;
using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu.Game;

public interface IGameLeftMenuViewModel : ILeftMenuViewModel
{

    public ILaunchButtonViewModel LaunchButton { get; }
    public IGame Game { get; set; }

}
