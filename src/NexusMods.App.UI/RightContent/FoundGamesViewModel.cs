using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.ViewModels;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent;

public class FoundGamesViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{
    private readonly IGameWidgetViewModel[] _installedGames = Array.Empty<IGameWidgetViewModel>();
    private readonly ILogger<FoundGamesViewModel> _logger;
    private readonly LoadoutManager _loadoutManager;

    private Subject<(GameInstallation Installation, string LoadOutName)> _createdLoadouts = new();
    private readonly IServiceProvider _provider;

    public IObservable<(GameInstallation Installation, string LoadOutNmae)>
        CreatedLoadouts => _createdLoadouts;

    public FoundGamesViewModel(ILogger<FoundGamesViewModel> logger, IServiceProvider provider, LoadoutManager loadoutManager)
    {
        _logger = logger;
        _loadoutManager = loadoutManager;
        _provider = provider;

        Games = Array.Empty<IGameWidgetViewModel>()
            .ToReadOnlyObservableCollection();

    }

    private async Task ManageGame(GameInstallation installation)
    {
        var name = _loadoutManager.FindName(installation);
        var manage = _loadoutManager.ManageGameAsync(installation, name);
        _createdLoadouts.OnNext((installation, name));
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
                vm.PrimaryButton = ReactiveCommand.Create(() => ManageGame(install));
                return vm;
            });
        Games = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(installed));

    }

    public void InitializeManual(IEnumerable<IGame> games)
    {
        InitializeFromFound(games);
    }
}
