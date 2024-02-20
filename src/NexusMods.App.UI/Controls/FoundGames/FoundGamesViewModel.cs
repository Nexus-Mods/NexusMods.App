using System.Collections.ObjectModel;
using Avalonia.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls.FoundGames;

[UsedImplicitly]
public class FoundGamesViewModel : AViewModel<IFoundGamesViewModel>, IFoundGamesViewModel
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly IServiceProvider _provider;
    private readonly IWindowManager _windowManager;

    public FoundGamesViewModel(IServiceProvider provider, IWindowManager windowManager, ILoadoutRegistry loadoutManager)
    {
        _loadoutRegistry = loadoutManager;
        _provider = provider;
        _windowManager = windowManager;

        Games = Array.Empty<IGameWidgetViewModel>().ToReadOnlyObservableCollection();
    }

    private async Task ManageGame(GameInstallation installation)
    {
        var name = _loadoutRegistry.SuggestName(installation);
        var marker = await _loadoutRegistry.Manage(installation, name);

        var loadoutId = marker.Id;

        Dispatcher.UIThread.Invoke(() =>
        {
            if (!_windowManager.TryGetActiveWindow(out var window)) return;
            var workspaceController = window.WorkspaceController;

            workspaceController.ChangeOrCreateWorkspaceByContext(
                context => context.LoadoutId == loadoutId,
                () => new PageData
                {
                    FactoryId = LoadoutGridPageFactory.StaticId,
                    Context = new LoadoutGridContext
                    {
                        LoadoutId = loadoutId
                    }
                },
                () => new LoadoutContext
                {
                    LoadoutId = loadoutId
                }
            );
        });
    }

    [Reactive]
    public ReadOnlyObservableCollection<IGameWidgetViewModel> Games { get; set; }

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
