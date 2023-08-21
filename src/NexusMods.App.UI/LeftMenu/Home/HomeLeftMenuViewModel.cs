using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Utilities;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.Localization;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.RightContent.MyGames;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Home;

public class HomeLeftMenuViewModel : AViewModel<IHomeLeftMenuViewModel>, IHomeLeftMenuViewModel
{
    static ReadOnlyObservableCollection<ILeftMenuItemViewModel> _empty = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(new ObservableCollection<ILeftMenuItemViewModel>());

    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items { get; private set; } = _empty;

    [Reactive]
    public IRightContentViewModel RightContent { get; set; } = Initializers.IRightContent;

    public HomeLeftMenuViewModel(IMyGamesViewModel myGamesViewModel, IFoundGamesViewModel foundGamesViewModel)
    {
        this.WhenActivated(disposable =>
        {
            var items = new ILeftMenuItemViewModel[]
            {
                new IconViewModel(() => Language.Newsfeed) { Icon = IconType.News, Activate = ReactiveCommand.Create(
                    () =>
                    {
                        RightContent = Initializers.IRightContent;
                    })}.DisposeWith(disposable),
                new IconViewModel(() => Language.MyGames) { Icon = IconType.Bookmark, Activate = ReactiveCommand.Create(
                    () =>
                    {
                        RightContent = myGamesViewModel;
                    }) }.DisposeWith(disposable),
                new IconViewModel(() => Language.BrowseGames) { Icon = IconType.Game, Activate = ReactiveCommand.Create(
                    () =>
                    {
                        RightContent = foundGamesViewModel;
                    })}.DisposeWith(disposable)
            };

            Items = new ReadOnlyObservableCollection<ILeftMenuItemViewModel>(
                new ObservableCollection<ILeftMenuItemViewModel>(items));
        });
    }
}
