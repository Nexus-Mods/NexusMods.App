using NexusMods.DataModel.Games;

namespace NexusMods.App.UI.LeftMenu.Game;

public interface IGameLeftMenuViewModel : ILeftMenuViewModel
{

    public ILeftMenuItemViewModel LaunchButton { get; }
    public IGame Game { get; set; }

}
