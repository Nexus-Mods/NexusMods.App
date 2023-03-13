using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.ViewModels;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent;

public class FoundGamesViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{
    private readonly IGame[] _games;
    private readonly IGameWidgetViewModel[] _installedGames;
    private readonly ILogger<FoundGamesViewModel> _logger;
    private readonly LoadoutManager _loadoutManager;

    private Subject<(GameInstallation Installation, string LoadOutName)> _createdLoadouts = new();
    public IObservable<(GameInstallation Installation, string LoadOutNmae)>
        CreatedLoadouts => _createdLoadouts;

    public FoundGamesViewModel(ILogger<FoundGamesViewModel> logger, IEnumerable<IGame> games, IServiceProvider provider, LoadoutManager loadoutManager)
    {
        _logger = logger;
        _games = games.ToArray();
        _loadoutManager = loadoutManager;
        var installed = _games
            .SelectMany(g => g.Installations)
            .Select(install =>
            {
                var vm = provider.GetRequiredService<IGameWidgetViewModel>();
                vm.Installation = install;
                vm.PrimaryButton = ReactiveCommand.Create(() => ManageGame(install));
                return vm;
            });
        Games = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(installed));
    }

    private async Task ManageGame(GameInstallation installation)
    {
        var name = _loadoutManager.FindName(installation);
        var manage = await _loadoutManager.ManageGameAsync(installation, name);
        _createdLoadouts.OnNext((installation, name));
    }

    public ReadOnlyObservableCollection<IGameWidgetViewModel> Games { get; }
}
