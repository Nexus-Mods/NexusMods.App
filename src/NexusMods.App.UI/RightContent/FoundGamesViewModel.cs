using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Routing;
using NexusMods.App.UI.Routing.Messages;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent;

public class FoundGamesViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{
    private readonly LoadoutRegistry _loadoutRegistry;
    private readonly IServiceProvider _provider;
    private readonly IRouter _router;

    public FoundGamesViewModel(IServiceProvider provider, LoadoutRegistry loadoutManager, IRouter router)
    {
        _router = router;
        _loadoutRegistry = loadoutManager;
        _provider = provider;

        Games = Array.Empty<IGameWidgetViewModel>()
            .ToReadOnlyObservableCollection();

    }

    private async Task ManageGame(GameInstallation installation)
    {
        var name = _loadoutRegistry.SuggestName(installation);
        var marker = await _loadoutRegistry.Manage(installation, name);
        _router.NavigateTo(new NavigateToLoadout(marker.Value));
    }

    [Reactive]
    public ReadOnlyObservableCollection<IGameWidgetViewModel> Games
    {
        get;
        set;
    }

    public void InitializeFromFound(IEnumerable<IGame> games)
    {
        var installed = games
            .SelectMany(g => g.Installations)
            .Select(install =>
            {
                var vm = _provider.GetRequiredService<IGameWidgetViewModel>();
                vm.Installation = install;
                vm.PrimaryButton = ReactiveCommand.CreateFromTask(async () =>
                {
                    await Task.Run(async () => await ManageGame(install));
                });
                return vm;
            });
        Games = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(installed));

    }

    public void InitializeManual(IEnumerable<IGame> games)
    {
        InitializeFromFound(games);
    }
}
