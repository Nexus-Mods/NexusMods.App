using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent;

public class FoundGamesViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{
    private readonly LoadoutManager _loadoutManager;
    private readonly IServiceProvider _provider;
    public FoundGamesViewModel(IServiceProvider provider, LoadoutManager loadoutManager)
    {
        _loadoutManager = loadoutManager;
        _provider = provider;

        Games = Array.Empty<IGameWidgetViewModel>()
            .ToReadOnlyObservableCollection();

    }

    private async Task ManageGame(GameInstallation installation)
    {
        var name = _loadoutManager.FindName(installation);
        var _ = await _loadoutManager.ManageGameAsync(installation, name);
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
                vm.PrimaryButton = ReactiveCommand.CreateFromTask(() => ManageGame(install));
                return vm;
            });
        Games = new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(installed));

    }

    public void InitializeManual(IEnumerable<IGame> games)
    {
        InitializeFromFound(games);
    }
}
