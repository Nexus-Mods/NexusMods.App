using System.Collections.ObjectModel;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.RightContent;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Game;

public class GameLeftMenuDesignViewModel : AViewModel<IGameLeftMenuViewModel>, IGameLeftMenuViewModel
{
    [Reactive] public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; set; }

    [Reactive] public ILaunchButtonViewModel LaunchButton { get; set; }

    [Reactive] public IGame Game { get; set; } = GameInstallation.Empty.Game;

    [Reactive] public IRightContentViewModel RightContent { get; set; } = Initializers.IRightContent;

    public GameLeftMenuDesignViewModel()
    {
        LaunchButton = new LaunchButtonDesignViewModel();

        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel { Name = "Newsfeed", Icon = IconType.News },
            new IconViewModel { Name = "My loadout 1", Icon = IconType.ChevronRight },
            new IconViewModel { Name = "My other loadout", Icon = IconType.ChevronRight },
        };
        Items = items.ToReadOnlyObservableCollection();
    }
}
