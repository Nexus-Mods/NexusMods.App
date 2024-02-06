using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent;

[UsedImplicitly]
public class FoundGamesViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly IServiceProvider _provider;

    public FoundGamesViewModel(IServiceProvider provider, ILoadoutRegistry loadoutManager)
    {
        _loadoutRegistry = loadoutManager;
        _provider = provider;

        Games = Array.Empty<IGameWidgetViewModel>()
            .ToReadOnlyObservableCollection();

    }

    private async Task ManageGame(GameInstallation installation)
    {
        var name = _loadoutRegistry.SuggestName(installation);
        var marker = await _loadoutRegistry.Manage(installation, name);
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
