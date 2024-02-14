using System.Collections.ObjectModel;
using JetBrains.Annotations;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Pages.MyGames;
using NexusMods.App.UI.Resources;
using ReactiveUI;

namespace NexusMods.App.UI.LeftMenu.Home;

[UsedImplicitly]
public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; }

    public HomeLeftMenuViewModel(IMyGamesViewModel myGamesViewModel)
    {
        var items = new ILeftMenuItemViewModel[]
        {
            new IconViewModel
            {
                Name = Language.MyGames, Icon = IconType.Bookmark, Activate = ReactiveCommand.Create(
                    () => throw new NotImplementedException("Navigate to workspace"))
            }
        };
        Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>(items));
    }
}
