using NexusMods.App.UI.ViewModels;
using NexusMods.DataModel.Games;

namespace NexusMods.App.UI.RightContent;

public class AddGameViewModel : AViewModel
{
    private readonly IGame _game;

    public AddGameViewModel(IGame game)
    {
        _game = game;
    }
    
}