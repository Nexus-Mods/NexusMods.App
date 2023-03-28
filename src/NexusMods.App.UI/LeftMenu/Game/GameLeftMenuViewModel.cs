using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Icons;
using NexusMods.App.UI.LeftMenu.Items;
using NexusMods.App.UI.RightContent;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.LeftMenu.Game;

public class GameLeftMenuViewModel : AViewModel<IGameLeftMenuViewModel>, IGameLeftMenuViewModel
{
    private ReadOnlyObservableCollection<ILeftMenuItemViewModel> _items =
        new(Array.Empty<ILeftMenuItemViewModel>().ToObservableCollection());

    public ReadOnlyObservableCollection<ILeftMenuItemViewModel> Items => _items;

    [Reactive]
    public ILeftMenuItemViewModel LaunchButton { get; set; }
    [Reactive] public IGame Game { get; set; } = GameInstallation.Empty.Game;

    [Reactive]
    public IRightContentViewModel RightContent { get; set; } = Initializers.IRightContent;

    public GameLeftMenuViewModel(ILogger<GameLeftMenuViewModel> logger, LoadoutRegistry loadoutRegistry, ILaunchButtonViewModel launchButton, IServiceProvider provider)
    {
        LaunchButton = launchButton;

        this.WhenActivated(d =>
        {
            var gameFilterFn = this.WhenAnyValue(vm => vm.Game)
                .Select<IGame, Func<Loadout, bool>>(game => loadout =>
                    loadout.Installation.Game.Domain == game.Domain);

            this.WhenAnyValue(vm => vm.Game)
                .BindTo(launchButton, vm => vm.Game)
                .DisposeWith(d);

            loadoutRegistry.Loadouts
                .Filter(gameFilterFn)
                .SortBy(list => list.Name)
                .Transform(loadout =>
                {
                    var vm = provider.GetRequiredService<IIconViewModel>();
                    vm.Icon = IconType.ChevronRight;
                    vm.Name = loadout.Name;
                    vm.Activate = ReactiveCommand.Create(() =>
                    {
                        logger.LogDebug("Loadout {LoadoutId} selected",
                            loadout.LoadoutId);
                    });
                    return (ILeftMenuItemViewModel)vm;
                })
                .OnUI()
                .Bind(out _items)
                .Subscribe()
                .DisposeWith(d);
        });
    }
}
