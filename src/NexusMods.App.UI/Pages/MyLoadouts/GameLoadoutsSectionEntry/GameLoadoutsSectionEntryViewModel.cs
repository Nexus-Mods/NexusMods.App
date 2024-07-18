using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Controls.LoadoutCard;
using NexusMods.App.UI.Pages.LoadoutGrid;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.MyLoadouts.GameLoadoutsSectionEntry;

public class GameLoadoutsSectionEntryViewModel : AViewModel<IGameLoadoutsSectionEntryViewModel>, IGameLoadoutsSectionEntryViewModel
{
    private readonly CompositeDisposable _compositeDisposable;
    private readonly GameInstallation _gameInstallation;
    private readonly IWindowManager _windowManager;
    private ReadOnlyObservableCollection<IViewModelInterface> _loadoutCardViewModels = new([]);
    private ReadOnlyObservableCollection<IViewModelInterface> _cardViewModels = new([]);
    private SourceList<IViewModelInterface> _cardViewModelsSourceList = new();
    public string HeadingText { get; }
    public ReadOnlyObservableCollection<IViewModelInterface> CardViewModels => _cardViewModels;


    public GameLoadoutsSectionEntryViewModel(GameInstallation gameInstallation, IConnection conn, IServiceProvider serviceProvider, IWindowManager windowManager)
    {
        _compositeDisposable = new CompositeDisposable();
        _gameInstallation = gameInstallation;
        _windowManager = windowManager;
        HeadingText = _gameInstallation.Game.Name + " Loadouts";

        Loadout.ObserveAll(conn)
            .Filter(l => l.IsVisible() && l.InstallationInstance.LocationsRegister[LocationId.Game] == _gameInstallation.LocationsRegister[LocationId.Game])
            .Transform(loadout =>
                {
                    return (IViewModelInterface)new LoadoutCardViewModel(loadout, conn, serviceProvider)
                    {
                        VisitLoadoutCommand = ReactiveCommand.Create(() => NavigateToLoadout(loadout)),
                        CloneLoadoutCommand = ReactiveCommand.Create(() =>
                        {
                            // TODO: Implement Loadout cloning
                        }),
                    };
                }
            )
            .Bind(out _loadoutCardViewModels)
            .Subscribe()
            .DisposeWith(_compositeDisposable);

        var CreateNewLoadoutCard = new CreateNewLoadoutCardViewModel()
        {
            AddLoadoutCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    await _gameInstallation.GetGame().Synchronizer.CreateLoadout(_gameInstallation);
                }
            ),
        };
        
        _cardViewModelsSourceList.Add(CreateNewLoadoutCard);
        
        _cardViewModelsSourceList.Connect()
            .Merge(_loadoutCardViewModels.ToObservableChangeSet())
            .OnUI()
            .Bind(out _cardViewModels)
            .SubscribeWithErrorLogging()
            .DisposeWith(_compositeDisposable);
    }


    private void NavigateToLoadout(Loadout.ReadOnly loadout)
    {
        var loadoutId = loadout.LoadoutId;
        Dispatcher.UIThread.Invoke(() =>
            {
                var workspaceController = _windowManager.ActiveWorkspaceController;

                workspaceController.ChangeOrCreateWorkspaceByContext(
                    context => context.LoadoutId == loadoutId,
                    () => new PageData
                    {
                        FactoryId = LoadoutGridPageFactory.StaticId,
                        Context = new LoadoutGridContext
                        {
                            LoadoutId = loadoutId,
                        },
                    },
                    () => new LoadoutContext
                    {
                        LoadoutId = loadoutId,
                    }
                );
            }
        );
    }

    public void Dispose()
    {
        _compositeDisposable.Dispose();
    }
}
