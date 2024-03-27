using System.Collections.ObjectModel;
using Avalonia.Threading;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.MyGames;

[UsedImplicitly]
public class MyGamesViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly IServiceProvider _provider;
    private readonly IWindowManager _windowManager;
    
    [Reactive] public ReadOnlyObservableCollection<IGameWidgetViewModel> ManagedGames { get; private set; }
    public ReadOnlyObservableCollection<IGameWidgetViewModel> DetectedGames { get; private set; }

    public MyGamesViewModel(
        IWindowManager windowManager,
        IServiceProvider serviceProvider,
        ILoadoutRegistry loadoutRegistry,
        IEnumerable<IGame> games) : base(windowManager)
    {
        _provider = serviceProvider;
        _loadoutRegistry = loadoutRegistry;
        _windowManager = windowManager;
        
        var gamesList = games.ToList();
        DetectedGames = GetDetectedGames(gamesList);
        // TODO: Implement ManagedGames
        ManagedGames = GetDetectedGames(gamesList);

        this.WhenActivated(() =>
        {
            GetWorkspaceController().SetTabTitle(Language.MyGames, WorkspaceId, PanelId, TabId);

            return Array.Empty<IDisposable>();
        });
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
    
    private ReadOnlyObservableCollection<IGameWidgetViewModel> GetDetectedGames(IEnumerable<IGame> games)
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
        return new ReadOnlyObservableCollection<IGameWidgetViewModel>(new ObservableCollection<IGameWidgetViewModel>(installed));
    }


}
